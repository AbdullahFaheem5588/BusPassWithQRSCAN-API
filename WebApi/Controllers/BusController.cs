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
        public HttpResponseMessage GetBusesLocations()
        {
            try
            {
                var buses = db.Buses.ToList();
                List<BusLocation> busLocationList = new List<BusLocation>();
                for (int i = 0; i < buses.Count; i++)
                {
                    var bus = buses.Skip(i).FirstOrDefault();
                    if (bus != null)
                    {
                        int busId = bus.id;
                        var locationFromDb = db.TracksLocations.Where(t => t.bus_id == busId && t.date == DateTime.Today).OrderByDescending(t => t.time).FirstOrDefault();
                        if (locationFromDb != null)
                        {
                            BusLocation busLocation = new BusLocation();
                            busLocation.BusId = busId;
                            var routeId = db.Starts.Where(s => s.date == DateTime.Today && s.bus_id == busId).OrderByDescending(s => s.time).Select(s => s.route_id).FirstOrDefault();
                            busLocation.RouteId = Convert.ToInt32(routeId);
                            busLocation.RouteTitle = db.Routes.Where(r => r.id == busLocation.RouteId).Select(r => r.Title).FirstOrDefault();
                            busLocation.Cords = new Location
                            {
                                latitude = Convert.ToDouble(locationFromDb.latitude),
                                longitude = Convert.ToDouble(locationFromDb.longitude),
                            };
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
