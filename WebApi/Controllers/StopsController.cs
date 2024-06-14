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
        public HttpResponseMessage GetAllRoutes(int OrganizationId)
        {
            try
            {
                var sharedRoutes = db.RouteSharings.Where(rs => rs.organization2_id == OrganizationId && rs.Status == "Accepted").Select(rs => rs.route_id).ToList();
                var stops = db.Stops.ToList();
                var routeStop = db.RouteStops.ToList();
                var route = db.Routes.Where(r => r.organization_id == OrganizationId).ToList();
                List<List<ApiStops>> apiRoute = new List<List<ApiStops>>();
                for (int i = 0; i < route.Count; i++)
                {
                    List<ApiStops> apiStops = new List<ApiStops>();
                    var stopsInRoute = routeStop.Where(rs => rs.route_id == route[i].id).Select(rs => new
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
                        apiStop.Route = route[i].id;
                        apiStops.Add(apiStop);
                    }
                    apiRoute.Add(apiStops);
                }
                for (int i = 0; i < sharedRoutes.Count; i++)
                {
                    List<ApiStops> apiStops = new List<ApiStops>();
                    var stopsInRoute = routeStop.Where(rs => rs.route_id == sharedRoutes[i]).Select(rs => new
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
                        apiStop.Route = Convert.ToInt32(sharedRoutes[i]);
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
        [HttpGet]
        public HttpResponseMessage GetAllStops()
        {
            try
            {
                var stops = db.Stops.ToList();
                List<ApiStops> apiStops = new List<ApiStops>();
                foreach (var stop in stops)
                {
                    apiStops.Add(new ApiStops
                    {
                        Id = stop.id,
                        Name = stop.name,
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, apiStops);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }

        }
        [HttpGet]
        public HttpResponseMessage GetAllRoutesTitle(int OrganizationId)
        {
            try
            {
                var routes = db.Routes.Where(r => r.organization_id == OrganizationId).ToList();
                List<Routes> apiRoute = new List<Routes>();
                for (int i = 0; i < routes.Count; i++)
                {
                    Routes route = new Routes
                    {
                        RouteId = routes[i].id,
                        RouteTitle = routes[i].Title,
                    };
                    apiRoute.Add(route);
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
