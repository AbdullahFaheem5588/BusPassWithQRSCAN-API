using System;
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
                var busId = db.Conductors.Where(c => c.id == conductorId)
                    .Join(db.Buses, c => c.id, b => b.conductor_id, (c, b) => b.id)
                    .SingleOrDefault();
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

                return nextStopDetails != null
                    ? Request.CreateResponse(HttpStatusCode.OK, nextStopDetails)
                    : Request.CreateResponse(HttpStatusCode.NotFound, "Next stop not found.");
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
