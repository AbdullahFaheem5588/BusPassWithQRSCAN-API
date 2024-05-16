using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class StopsController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetAllStops()
        {
            try
            {
                var stops = db.Stops.ToList();
                var routeStop = db.RouteStops.ToList();
                var route = db.Routes.ToList();
                List<List<ApiStops>> apiRoute = new List<List<ApiStops>>();
                for(int i=0; i<route.Count; i++)
                {
                    List<ApiStops> apiStops = new List<ApiStops>();
                    var stopsInRoute = routeStop.Where(rs => rs.route_id == route[i].id).Select(rs => new
                    {
                        StopId = rs.stop_id,
                        StopTiming = rs.stoptiming,
                    }).ToList();
                    for(int j=0; j<stopsInRoute.Count; j++)
                    {
                        ApiStops apiStop = new ApiStops();
                        apiStop.Id = Convert.ToInt32(stopsInRoute[j].StopId);
                        apiStop.Name = stops.FirstOrDefault(s => s.id == apiStop.Id)?.name;
                        apiStop.Timing = stopsInRoute[j].StopTiming.ToString();
                        apiStop.Latitude = stops.FirstOrDefault(s => s.id == apiStop.Id)?.latitude;
                        apiStop.Longitude = stops.FirstOrDefault(s => s.id == apiStop.Id)?.longitude;
                        apiStop.Route = route[i].id;
                        apiStops.Add(apiStop);
                    }
                    apiRoute.Add(apiStops);
                }
                return Request.CreateResponse(HttpStatusCode.OK, apiRoute);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }

        }
    }
}
