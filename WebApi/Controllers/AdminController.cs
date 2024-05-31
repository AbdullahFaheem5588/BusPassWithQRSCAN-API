using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class AdminController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetBusDetails()
        {
            try
            {
                var busDetailsList = new List<object>();

                var buses = db.Buses.ToList();

                foreach (var bus in buses)
                {
                    var busId = bus.id;
                    var totalSeats = bus.totalSeats;

                    var startCount = (from s in db.Starts
                                      where s.bus_id == busId && s.date == DateTime.Today
                                      select s).Count();

                    int bookedSeats = 0;

                    if (startCount == 1)
                    {
                        bookedSeats = ((from t in db.Travels
                                        where t.bus_id == busId && t.date == DateTime.Today
                                        && t.type.Contains("pickup_checkin")
                                        select t).Count()) - ((from t in db.Travels
                                                               where t.bus_id == busId && t.date == DateTime.Today
                                                               && t.type.Contains("pickup_checkout")
                                                               select t).Count());
                    }
                    else if (startCount == 2)
                    {
                        bookedSeats = ((from t in db.Travels
                                        where t.bus_id == busId && t.date == DateTime.Today
                                        && t.type.Contains("dropoff_checkin")
                                        select t).Count()) - ((from t in db.Travels
                                                               where t.bus_id == busId && t.date == DateTime.Today
                                                               && t.type.Contains("dropoff_checkout")
                                                               select t).Count());
                    }

                    int totalStops = 0;
                    int remainingStops = 0;

                    var starts = db.Starts.Where(s => s.date == DateTime.Today && s.bus_id == busId)
                                          .OrderByDescending(s => s.id).FirstOrDefault();

                    if (starts != null)
                    {
                        JourneyStopsChecker journeyStopsChecker = new JourneyStopsChecker();
                        totalStops = db.RouteStops.Where(rs => rs.route_id == starts.route_id).Count();
                        var time = DateTime.Now.TimeOfDay;
                        remainingStops = totalStops - db.Reaches
                            .Where(r => r.date == DateTime.Today && time >= r.time && r.route_id == starts.route_id && r.stop_id != null)
                            .Count();
                    }

                    busDetailsList.Add(new
                    {
                        BusId = busId,
                        TotalSeats = totalSeats,
                        BookedSeats = bookedSeats,
                        TotalStops = totalStops,
                        RemainingStops = remainingStops
                    });
                }

                return Request.CreateResponse(HttpStatusCode.OK, busDetailsList);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }


    }
}
