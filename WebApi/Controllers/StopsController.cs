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
        //[HttpGet]
        //public HttpResponseMessage GetAllStop()
        //{
        //    try
        //    {
        //        var stops = db.Stops.ToList();
        //        var routeStop = db.RouteStops.ToList();
        //        List<ApiStops> apiStops = stops.Select(stop => new ApiStops
        //        {
        //            Id = stop.id,
        //            Name = stop.name,
        //            StopNo = Convert.ToInt32(stop.stopno),
        //            Pickup = stop.pickup.ToString(),
        //            Dropup = stop.dropup.ToString(),
        //            Logitude = stop.longitude,
        //            Latitude = stop.latitude,
        //            Route = Convert.ToInt32(routeStop.Where(r => r.stop_id == stop.id).Select(u => u.route_id).FirstOrDefault()),
        //        }).ToList();
        //        return Request.CreateResponse(HttpStatusCode.OK, res);
        //    }
        //    catch
        //    {

        //        return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
        //    }

        //}
    }
}
