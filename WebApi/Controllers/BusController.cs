using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class BusController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetBusesLocations(int OrganizationId)
        {
            try
            {
                var buses = db.Buses.Where(b => b.organization_id == OrganizationId).ToList();
                List<BusLocation> busLocationList = new List<BusLocation>();
                for (int i = 0; i < buses.Count; i++)
                {
                    var bus = buses[i];
                    if (bus != null)
                    {
                        int busId = bus.id;
                        var startCount = (from s in db.Starts
                                          where s.bus_id == busId && s.date == DateTime.Today
                                          select s).Count();
                        int bookedSeats = 0;
                        if (startCount > 0)
                        {
                            bookedSeats = ((from t in db.Travels
                                            where t.bus_id == busId && t.date == DateTime.Today
                                            && (t.type.Contains("pickup_checkin") || t.type.Contains("dropoff_checkin"))
                                            select t).Count()) - ((from t in db.Travels
                                                                   where t.bus_id == busId && t.date == DateTime.Today && (t.type.Contains("pickup_checkout") || t.type.Contains("dropoff_checkout"))
                                                                   select t).Count());
                        }
                        var locationFromDb = db.TracksLocations.Where(t => t.bus_id == busId && t.date == DateTime.Today).OrderByDescending(t => t.time).FirstOrDefault();
                        if (locationFromDb != null)
                        {
                            BusLocation busLocation = new BusLocation();
                            busLocation.BusId = busId;
                            busLocation.TotalSeats = Convert.ToInt32(db.Buses.Where(b => b.id == busId).Select(b => b.totalSeats).FirstOrDefault());
                            busLocation.Passengers = bookedSeats;
                            var routeId = db.Starts.Where(s => s.date == DateTime.Today && s.bus_id == busId).OrderByDescending(s => s.time).Select(s => s.route_id).FirstOrDefault();
                            busLocation.RouteId = Convert.ToInt32(routeId);
                            busLocation.RouteTitle = db.Routes.Where(r => r.id == busLocation.RouteId).Select(r => r.Title).FirstOrDefault();
                            busLocation.Cords = new Location
                            {
                                latitude = Convert.ToDouble(locationFromDb.latitude),
                                longitude = Convert.ToDouble(locationFromDb.longitude),
                            };
                            List<PassengersDetails> passengersDetails = new List<PassengersDetails>();
                            if (bookedSeats > 0)
                            {
                                var passengersDetailsFromDB = db.Travels.Where(t => t.date == DateTime.Today && t.bus_id == busId && t.route_id == routeId && (t.type == "pickup_checkin" || t.type == "dropoff_checkin")).OrderByDescending(s => s.time).ToList();
                                for (int j = 0; j < passengersDetailsFromDB.Count; j++)
                                {
                                    int passId = Convert.ToInt32(passengersDetailsFromDB[j].pass_id);
                                    var passengerDetails = new PassengersDetails();
                                    passengerDetails.Name = db.Students.Where(s => s.pass_id == passId).Select(s => s.name).FirstOrDefault();
                                    passengerDetails.RegNo = db.Students.Where(s => s.pass_id == passId).Select(s => s.regno).FirstOrDefault();
                                    passengerDetails.PassId = passId;
                                    if (passengersDetailsFromDB[j].stop_id != null || passengersDetailsFromDB[j].stop_id != 0)
                                    {

                                        int stopId = Convert.ToInt32(passengersDetailsFromDB[j].stop_id);
                                        passengerDetails.StopName = db.Stops.Where(s => s.id == stopId).Select(s => s.name).FirstOrDefault();
                                    }
                                    else
                                    {
                                        passengerDetails.StopName = "Uni";
                                    }

                                    passengersDetails.Add(passengerDetails);
                                }
                                busLocation.PassengersDetails = passengersDetails;
                            }
                            busLocationList.Add(busLocation);
                        }
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, busLocationList);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
    }
}
