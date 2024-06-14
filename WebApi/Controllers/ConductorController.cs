using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class ConductorController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetNextStop(int conductorId)
        {
            try
            {
                var OrganizationId = (from u in db.Users join c in db.Conductors on u.id equals c.user_id where (c.id == conductorId) select (u.organization_id)).FirstOrDefault();
                var busId = db.Conductors.Where(c => c.id == conductorId)
                    .Join(db.Buses, c => c.id, b => b.conductor_id, (c, b) => b.id)
                    .FirstOrDefault();

                if (busId == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Bus not found for the given conductor.");
                }

                var routeId = db.Starts.Where(s => s.bus_id == busId && s.date == DateTime.Today)
                    .OrderByDescending(s => s.time)
                    .Select(s => s.route_id)
                    .FirstOrDefault();

                if (routeId == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Route not found for the given bus and date.");
                }

                var currentLocation = GetCurrentBusLocation(busId);

                if (currentLocation == null)
                {
                    return Request.CreateResponse(HttpStatusCode.InternalServerError, "Unable to get the current location of the bus.");
                }

                var reachedStopIds = db.Reaches
                    .Where(r => r.route_id == routeId && r.date == DateTime.Today)
                    .Select(r => r.stop_id)
                    .ToList();

                var stopsInRoute = db.RouteStops
                    .Where(rs => rs.route_id == routeId && !reachedStopIds.Contains(rs.stop_id))
                    .Join(db.Stops, rs => rs.stop_id, s => s.id, (rs, s) => new { RouteStop = rs, Stop = s })
                    .ToList();

                if (!stopsInRoute.Any())
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No remaining stops found for the route.");
                }

                var nextStop = stopsInRoute
                    .OrderBy(s => GetDistance(currentLocation.latitude, currentLocation.longitude, Convert.ToDouble(s.Stop.latitude), Convert.ToDouble(s.Stop.longitude)))
                    .FirstOrDefault();

                if (nextStop == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Next stop not found.");
                }

                var nextStopDetails = new ApiStops
                {
                    Id = nextStop.Stop.id,
                    Name = nextStop.Stop.name,
                    Timing = nextStop.RouteStop.stoptiming.ToString(),
                    Route = routeId.Value
                };
                if (nextStopDetails != null)
                {
                    List<int> listOfIds = (from u in db.Users
                                           join s in db.Students on u.id equals s.user_id
                                           join f in db.FavouriteStops on s.id equals f.student_id
                                           where f.stop_id == nextStopDetails.Id && u.organization_id == Convert.ToInt32(OrganizationId)
                                           select u.id).ToList();
                    UsersController usersController = new UsersController();
                    for (int i = 0; i < listOfIds.Count; i++)
                    {
                        var description = ("Bus No " + busId + " is Enroute to your Favourite Stop: " + nextStopDetails.Name);
                        var duplicationCheck = db.Notifications.Where(n => n.user_id == listOfIds[i] && n.description == description
                        && n.time > db.Starts.Where(s => s.bus_id == busId && s.date == DateTime.Today).OrderByDescending(s => s.time).Select(s => s.time)
                        .FirstOrDefault());
                        if (duplicationCheck == null)
                            usersController.LocalNotifyUser(listOfIds[i], "Bus En-route!", description);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, nextStopDetails);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        private Location GetCurrentBusLocation(int busId)
        {
            var latestLocation = db.TracksLocations
                                  .Where(t => t.bus_id == busId && t.date == DateTime.Today)
                                  .OrderByDescending(t => t.time)
                                  .FirstOrDefault();

            if (latestLocation == null)
            {
                return null;
            }

            return new Location
            {
                latitude = Convert.ToDouble(latestLocation.latitude),
                longitude = Convert.ToDouble(latestLocation.longitude)
            };
        }
        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371;
            var latDistance = ToRadians(lat2 - lat1);
            var lonDistance = ToRadians(lon2 - lon1);
            var a = Math.Sin(latDistance / 2) * Math.Sin(latDistance / 2)
                    + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2))
                    * Math.Sin(lonDistance / 2) * Math.Sin(lonDistance / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }
        private double ToRadians(double angle)
        {
            return angle * (Math.PI / 180);
        }
        [HttpGet]
        public HttpResponseMessage GetBookedSeats(int conductorId)
        {
            try
            {
                var startCount = (from s in db.Starts
                                  join b in db.Buses on s.bus_id equals b.id
                                  where b.conductor_id == conductorId && s.date == DateTime.Today
                                  select s).Count();
                int bookedSeats = 0;
                if (startCount == 1)
                {
                    bookedSeats = ((from t in db.Travels
                                    join b in db.Buses on t.bus_id equals b.id
                                    where b.conductor_id == conductorId && t.date == DateTime.Today
                                    && t.type.Contains("pickup_checkin")
                                    select t).Count()) - ((from t in db.Travels
                                                           join b in db.Buses on t.bus_id equals b.id
                                                           where b.conductor_id == conductorId
                                                           && t.date == DateTime.Today && t.type.Contains("pickup_checkout")
                                                           select t).Count());
                }
                else if (startCount == 2)
                {
                    bookedSeats = ((from t in db.Travels
                                    join b in db.Buses on t.bus_id equals b.id
                                    where b.conductor_id == conductorId && t.date == DateTime.Today
                                    && t.type.Contains("dropoff_checkin")
                                    select t).Count()) - ((from t in db.Travels
                                                           join b in db.Buses on t.bus_id equals b.id
                                                           where b.conductor_id == conductorId
                                                           && t.date == DateTime.Today && t.type.Contains("dropoff_checkout")
                                                           select t).Count());
                }
                return Request.CreateResponse(HttpStatusCode.OK, bookedSeats);
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
        [HttpGet]
        public HttpResponseMessage IsJourneyCompleted(int conductorId)
        {
            try
            {
                var journeyStart = (from s in db.Starts
                                    join b in db.Buses on s.bus_id equals b.id
                                    where s.date == DateTime.Today && b.conductor_id == conductorId
                                    orderby s.time descending
                                    select s).FirstOrDefault();
                bool journeyCompleted = false;
                if (journeyStart != null)
                {
                    var reachedCount = (from r in db.Reaches
                                        where r.date == DateTime.Today && r.bus_id == journeyStart.bus_id && r.time >= journeyStart.time && r.stop_id == null && r.route_id == journeyStart.route_id
                                        select r).Count();
                    if (reachedCount == 2)
                    {
                        journeyCompleted = true;
                    }
                }
                else
                {
                    journeyCompleted = true;
                }
                return Request.CreateResponse(HttpStatusCode.OK, journeyCompleted);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetStartedRoute(int conductorId)
        {
            try
            {
                var routeId = (from s in db.Starts
                               join b in db.Buses on s.bus_id equals b.id
                               where s.date == DateTime.Today && b.conductor_id == conductorId
                               orderby s.time descending
                               select s.route_id).FirstOrDefault();
                if (routeId == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No route found for the given conductor today.");
                }
                var stops = db.Stops.ToList();
                var stopsInRoute = db.RouteStops
                                     .Where(rs => rs.route_id == routeId)
                                     .Select(rs => new
                                     {
                                         StopId = rs.stop_id,
                                         StopTiming = rs.stoptiming
                                     }).ToList();
                var apiStops = stopsInRoute.Select(sir => new ApiStops
                {
                    Id = Convert.ToInt32(sir.StopId),
                    Name = stops.FirstOrDefault(s => s.id == sir.StopId)?.name,
                    Timing = sir.StopTiming.ToString(),
                    Latitude = stops.FirstOrDefault(s => s.id == sir.StopId)?.latitude,
                    Longitude = stops.FirstOrDefault(s => s.id == sir.StopId)?.longitude,
                    Route = Convert.ToInt32(routeId)
                }).ToList();
                var routeTitle = db.Routes.Where(r => r.id == routeId).Select(r => r.Title).FirstOrDefault();
                var routeDetails = new
                {
                    RouteId = routeId,
                    RouteTitle = routeTitle,
                    Stops = apiStops
                };
                return Request.CreateResponse(HttpStatusCode.OK, routeDetails);
            }
            catch (Exception)
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error retrieving started route details.");
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
                db.SaveChanges();
                ReachedAtStop(busId, routeId, 0);
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
                var starts = db.Starts.Where(s => s.date == DateTime.Today && s.bus_id == busId && s.route_id == routeId).OrderByDescending(s => s.time).FirstOrDefault();
                if (starts != null)
                {
                    var recordCount = db.Reaches.Where(r => r.date == DateTime.Today && r.time >= starts.time && r.bus_id == busId && r.route_id == routeId && r.stop_id == null).Count();
                    if (stopId != 0)
                        recordCount = db.Reaches.Where(r => r.date == DateTime.Today && r.time >= starts.time && r.bus_id == busId && r.route_id == routeId && r.stop_id == stopId).Count();
                    var lastRecord = db.Reaches.Where(r => r.date == DateTime.Today && r.time >= starts.time && r.bus_id == busId && r.route_id == routeId).OrderByDescending(r => r.time).FirstOrDefault();
                    if ((recordCount == 0) || (recordCount == 1 && stopId == 0 && lastRecord.stop_id != null))
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
                        db.Reaches.Add(reached);
                        db.SaveChanges();
                        var routeStop = db.RouteStops.FirstOrDefault(rs => rs.route_id == routeId && rs.stop_id == stopId);

                        var reachTimes = db.Reaches.Where(r => r.route_id == routeId && r.stop_id == stopId)
                        .Select(r => r.time).ToList();

                        if (reachTimes.Any())
                        {
                            double averageTime = reachTimes.Average(time => time.Value.TotalSeconds);
                            routeStop.stoptiming = TimeSpan.FromSeconds(averageTime);
                            db.SaveChanges();
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, "Reached at Stop: " + reached.stop_id);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, "History Already Managed");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No Journey Started for this Bus!");
                }
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
                if (busLocation.Cords.latitude != 0 && busLocation.Cords.longitude != 0)
                {
                    var tracksLocation = new TracksLocation
                    {
                        date = DateTime.Today,
                        time = DateTime.Now.TimeOfDay,
                        longitude = busLocation.Cords.longitude.ToString(),
                        latitude = busLocation.Cords.latitude.ToString(),
                        bus_id = busLocation.BusId,
                        route_id = (from s in db.Starts
                                    where s.date == DateTime.Today && s.bus_id == busLocation.BusId
                                    orderby s.time descending
                                    select s.route_id).FirstOrDefault(),
                    };
                    if (tracksLocation.route_id != null)
                    {
                        db.TracksLocations.Add(tracksLocation);
                        db.SaveChanges();
                        return Request.CreateResponse(HttpStatusCode.OK, "Bus Location Updated");
                    }
                    else
                    {

                        return Request.CreateResponse(HttpStatusCode.OK, "Invalid Route!");
                    }

                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Incorrect Bus Location");
                }
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
                var startedRouteId = db.Starts.Where(s => s.bus_id == busId && s.date == DateTime.Today).OrderByDescending(s => s.id).FirstOrDefault().route_id;
                if (startedRouteId != null)
                {
                    var busOrganizationId = db.Buses.Where(b => b.id == busId).Select(b => b.organization_id).FirstOrDefault();
                    var studentOrganizationId = (from u in db.Users join s in db.Students on u.id equals s.user_id where s.pass_id == passId select u.organization_id).FirstOrDefault();
                    var sharedRouteChecher = db.RouteSharings.Where(rs => rs.organization1_id == busOrganizationId && rs.organization2_id == studentOrganizationId && rs.route_id == startedRouteId && rs.Status == "Accepted").Count();
                    var notificationType = "";
                    var passDetails = db.Passes.Find(passId);
                    var studentDetails = db.Students.Where(s => s.pass_id == passId).FirstOrDefault();
                    int travelRecordCount = db.Travels.Where(t => t.pass_id == passId && t.date == DateTime.Today).Count();
                    string notificationDescription = "";
                    ApiStudent apiStudent = new ApiStudent();
                    if (passDetails != null && studentDetails != null && (busOrganizationId == studentOrganizationId || sharedRouteChecher == 1))
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
                                route_id = startedRouteId,
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
                                notificationType = "Check In!";
                                notificationDescription = "Checked-In to Bus No " + busId + " following Route No " + travel.route_id;
                                passDetails.remainingjourneys--;
                            }
                            else if (travelRecordCount == 3)
                            {
                                travel.type = "dropoff_checkout";
                                notificationType = "Check Out!";
                                notificationDescription = "Checked-Out of Bus No " + busId + " following Route No " + travel.route_id;
                                if (passDetails.remainingjourneys == 0)
                                    passDetails.status = "In-Active";
                            }
                            db.Travels.Add(travel);
                            db.SaveChanges();
                            UsersController usersController = new UsersController();
                            usersController.LocalNotifyUser(Convert.ToInt32(studentDetails.user_id), notificationType, notificationDescription);
                            int parentUserId = Convert.ToInt32(db.Parents.Where(p => p.id == studentDetails.parent_id).Select(p => p.user_id).FirstOrDefault());
                            usersController.LocalNotifyUser(parentUserId, notificationType, studentDetails.name + " " + notificationDescription);
                        }

                        return Request.CreateResponse(HttpStatusCode.OK, apiStudent);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No Student Found!");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Journey Started!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

    }
}
