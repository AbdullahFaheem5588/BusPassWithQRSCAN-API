using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class ConductorController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        UsersController usersController = new UsersController();
        [HttpGet]
        public HttpResponseMessage GetNextStop(int conductorId)
        {
            try
            {
                var busId = db.Conductors.Where(c => c.id == conductorId)
                    .Join(db.Buses, c => c.id, b => b.conductor_id, (c, b) => b.id)
                    .FirstOrDefault();
                if (busId == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Bus not found for the given conductor.");
                }

                var routeId = db.Starts.Where(s => s.bus_id == busId && s.date == DateTime.Today)
                    .OrderByDescending(s => s.id)
                    .Select(s => s.route_id)
                    .FirstOrDefault();

                if (routeId == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Route not found for the given bus and date.");
                }

                var lastReachedStopId = db.Reaches.Where(r => r.route_id == routeId && r.date == DateTime.Today)
                    .OrderByDescending(r => r.id)
                    .Select(r => r.stop_id)
                    .FirstOrDefault();

                var nextStopDetails = db.RouteStops
                .Where(rs => rs.route_id == routeId && rs.id > (db.RouteStops.Where(rs2 => rs2.route_id == routeId && rs2.stop_id == (lastReachedStopId ?? 0)).Select(rs2 => rs2.id).FirstOrDefault()))
                .Join(db.Stops,
                    rs => rs.stop_id,
                    s => s.id,
                    (rs, s) => new { RouteStop = rs, Stop = s }).Select(stopData => new ApiStops
                    {
                        Id = stopData.Stop.id,
                        Name = stopData.Stop.name,
                        Latitude = stopData.Stop.latitude,
                        Longitude = stopData.Stop.longitude,
                        Timing = stopData.RouteStop.stoptiming.ToString(),
                        Route = routeId.Value
                    })
                    .FirstOrDefault();
                Task.Run(() =>
                {
                    if (nextStopDetails != null)
                    {
                        List<int> listOfIds = (from u in db.Users
                                               join s in db.Students on u.id equals s.user_id
                                               join f in db.FavouriteStops on s.id equals f.student_id
                                               where f.stop_id == nextStopDetails.Id
                                               select u.id).ToList();
                        for (int i = 0; i < listOfIds.Count; i++)
                        {
                            usersController.NotifyUser(listOfIds[i], "Bus En-route!", ("Bus No " + busId + " is Enroute to your Favourite Stop: " + nextStopDetails.Name));
                        }
                    }
                });

                return nextStopDetails != null
                    ? Request.CreateResponse(HttpStatusCode.OK, nextStopDetails)
                    : Request.CreateResponse(HttpStatusCode.NotFound, "Next stop not found.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetRemainingSeats(int conductorId)
        {
            try
            {
                var startCount = (from s in db.Starts
                                  join b in db.Buses on s.bus_id equals b.id
                                  where b.conductor_id == conductorId && s.date == DateTime.Today
                                  select s).Count();
                SeatsAvailibility seats = new SeatsAvailibility();
                seats.TotalSeats = Convert.ToInt32(db.Buses.Where(b => b.conductor_id == conductorId).Select(b => b.totalSeats).FirstOrDefault());
                seats.StudentsCheckedIn = 0;
                if (startCount == 1)
                {
                    seats.StudentsCheckedIn = ((from t in db.Travels
                                                join b in db.Buses on t.bus_id equals b.id
                                                where b.conductor_id == conductorId && t.date == new DateTime(2024, 5, 18)
                                                && t.type.Contains("pickup_checkin")
                                                select t).Count()) - ((from t in db.Travels
                                                                       join b in db.Buses on t.bus_id equals b.id
                                                                       where b.conductor_id == conductorId
                                                                       && t.date == new DateTime(2024, 5, 18) && t.type.Contains("pickup_checkout")
                                                                       select t).Count());
                }
                else if (startCount == 2)
                {
                    seats.StudentsCheckedIn = ((from t in db.Travels
                                                join b in db.Buses on t.bus_id equals b.id
                                                where b.conductor_id == conductorId && t.date == new DateTime(2024, 5, 18)
                                                && t.type.Contains("dropoff_checkin")
                                                select t).Count()) - ((from t in db.Travels
                                                                       join b in db.Buses on t.bus_id equals b.id
                                                                       where b.conductor_id == conductorId
                                                                       && t.date == new DateTime(2024, 5, 18) && t.type.Contains("dropoff_checkout")
                                                                       select t).Count());
                }
                return Request.CreateResponse(HttpStatusCode.OK, seats);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetRemainingStops(int conductorId)
        {
            try
            {
                var busId = db.Conductors.Where(c => c.id == conductorId)
                    .Join(db.Buses, c => c.id, b => b.conductor_id, (c, b) => b.id)
                    .FirstOrDefault();
                var starts = db.Starts.Where(s => s.date == DateTime.Today && s.bus_id == busId).OrderByDescending(s => s.id).FirstOrDefault();
                JourneyStopsChecker journeyStopsChecker = new JourneyStopsChecker();
                journeyStopsChecker.TotalStops = db.RouteStops.Where(rs => rs.route_id == starts.route_id).Count();
                var time = DateTime.Now.TimeOfDay;
                journeyStopsChecker.RemainingStops = journeyStopsChecker.TotalStops - db.Reaches.Where(r => r.date == DateTime.Today && time >= r.time && r.route_id == starts.route_id && r.stop_id != null).Count();
                return Request.CreateResponse(HttpStatusCode.OK, journeyStopsChecker);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetAssignedRoutes(int conductorId)
        {
            try
            {
                var stops = db.Stops.ToList();
                var routeStopList = (from ia in db.IsAssigneds
                                     join b in db.Buses on ia.bus_id equals b.id
                                     where b.conductor_id == conductorId
                                     select ia).ToList();
                List<Routes> routes = new List<Routes>();
                for (int i = 0; i < routeStopList.Count; i++)
                {
                    int routeId = Convert.ToInt32(routeStopList[i].route_id);
                    List<ApiStops> apiStops = new List<ApiStops>();
                    var stopsInRoute = db.RouteStops.Where(rs => rs.route_id == routeId).Select(rs => new
                    {
                        StopId = rs.stop_id,
                        StopTiming = rs.stoptiming,
                    }).ToList();
                    for (int j = 0; j < stopsInRoute.Count; j++)
                    {
                        ApiStops apiStop = new ApiStops();
                        apiStop.Id = Convert.ToInt32(stopsInRoute[j].StopId);
                        apiStop.Name = stops.FirstOrDefault(s => s.id == apiStop.Id)?.name;
                        apiStop.Timing = stopsInRoute[j].StopTiming.ToString();
                        apiStop.Latitude = stops.FirstOrDefault(s => s.id == apiStop.Id)?.latitude;
                        apiStop.Longitude = stops.FirstOrDefault(s => s.id == apiStop.Id)?.longitude;
                        apiStop.Route = routeStopList[i].id;
                        apiStops.Add(apiStop);
                    }
                    routes.Add(new Routes
                    {
                        RouteId = routeId,
                        RouteTitle = db.Routes.Where(r => r.id == routeId).Select(r => r.Title).FirstOrDefault(),
                        Stops = apiStops,
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, routes);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage StartJourney(int busId, int routeId)
        {
            try
            {
                var startJourney = new Start
                {
                    date = DateTime.Today,
                    time = DateTime.Now.TimeOfDay,
                    bus_id = busId,
                    route_id = routeId,
                };
                db.Starts.Add(startJourney);
                ReachedAtStop(busId, routeId, 0);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Journey Started Succesfully");
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage ReachedAtStop(int busId, int routeId, int stopId)
        {
            try
            {
                var reached = new Reach
                {
                    date = DateTime.Today,
                    time = DateTime.Now.TimeOfDay,
                    bus_id = busId,
                    stop_id = stopId,
                    route_id = routeId,
                };
                if (stopId == 0)
                    reached.stop_id = null;
                else
                {
                    Task.Run(() =>
                    {
                        List<int> listOfIds = (from u in db.Users
                                               join s in db.Students on u.id equals s.user_id
                                               join f in db.FavouriteStops on s.id equals f.student_id
                                               where f.stop_id == stopId
                                               select u.id).ToList();
                        for (int i = 0; i < listOfIds.Count; i++)
                        {
                            usersController.NotifyUser(listOfIds[i], "Bus Arrived!", ("Bus No " + busId + " has reached at your Favourite Stop: " + db.Stops.Find(stopId).name));
                        }
                    });
                }
                db.Reaches.Add(reached);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Reaches at Stop: " + reached.stop_id);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage UpdateBusLocation(BusLocation busLocation)
        {
            try
            {
                var tracksLocation = new TracksLocation
                {
                    date = DateTime.Today,
                    time = DateTime.Now.TimeOfDay,
                    longitude = busLocation.Cords.longitude.ToString(),
                    latitude = busLocation.Cords.latitude.ToString(),
                    bus_id = busLocation.BusId,
                    route_id = busLocation.RouteId,
                };
                db.TracksLocations.Add(tracksLocation);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Bus's New Location Tracked");
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpGet]
        public HttpResponseMessage ScanQrCode(int passId, int busId)
        {
            try
            {
                var passDetails = db.Passes.Find(passId);
                var studentDetails = db.Students.Where(s => s.pass_id == passId).FirstOrDefault();
                int travelRecordCount = db.Travels.Where(t => t.pass_id == passId && t.date == DateTime.Today).Count();
                string notificationType = "";
                string notificationDescription = "";
                ApiStudent apiStudent = new ApiStudent();
                if (passDetails != null && studentDetails != null)
                {
                    apiStudent.Name = studentDetails.name;
                    apiStudent.RegNo = studentDetails.regno;
                    apiStudent.RemainingJourneys = Convert.ToInt32(passDetails.remainingjourneys);
                    apiStudent.Gender = studentDetails.gender;
                    apiStudent.PassId = Convert.ToInt32(studentDetails.pass_id);
                    apiStudent.PassExpiry = passDetails.passexpiry.ToString();
                    apiStudent.PassStatus = passDetails.status;

                    if (passDetails.status == "Active")
                    {
                        Travel travel = new Travel
                        {
                            date = DateTime.Today,
                            time = DateTime.Now.TimeOfDay,
                            pass_id = passDetails.id,
                            student_id = studentDetails.id,
                            bus_id = busId,
                            route_id = db.Starts.Where(s => s.bus_id == busId && s.date == DateTime.Today).OrderByDescending(s => s.id).FirstOrDefault().route_id,
                            stop_id = db.Reaches.Where(r => r.bus_id == busId && r.date == DateTime.Today).OrderByDescending(r => r.id).FirstOrDefault().stop_id,
                        };
                        if (travelRecordCount == 0)
                        {
                            travel.type = "pickup_checkin";
                            notificationType = "Check In!";
                            notificationDescription = "Checked-In to Bus No " + busId + " following Route No " + travel.route_id;
                            passDetails.remainingjourneys--;
                        }
                        else if (travelRecordCount == 1)
                        {
                            travel.type = "pickup_checkout";
                            notificationType = "Check Out!";
                            notificationDescription = "Checked-Out of Bus No " + busId + " following Route No " + travel.route_id;
                            if (passDetails.remainingjourneys == 0)
                                passDetails.status = "In-Active";
                        }
                        else if (travelRecordCount == 2)
                        {
                            travel.type = "dropoff_checkin";
                            notificationDescription = "Checked-In to Bus No " + busId + " following Route No " + travel.route_id;
                            notificationType = "Check In!";
                            passDetails.remainingjourneys--;
                        }
                        else if (travelRecordCount == 3)
                        {
                            travel.type = "dropoff_checkout";
                            notificationDescription = "Checked-Out of Bus No " + busId + " following Route No " + travel.route_id;
                            notificationType = "Check Out!";
                            if (passDetails.remainingjourneys == 0)
                                passDetails.status = "In-Active";
                        }
                        db.Travels.Add(travel);
                        usersController.NotifyUser(Convert.ToInt32(studentDetails.user_id), notificationType, notificationDescription);
                        int parentUserId = Convert.ToInt32(db.Parents.Where(p => p.id == studentDetails.parent_id).Select(p => p.user_id));
                        usersController.NotifyUser(parentUserId, notificationType, studentDetails.name + " " + notificationDescription);
                        db.SaveChanges();
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, apiStudent);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Student Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}
