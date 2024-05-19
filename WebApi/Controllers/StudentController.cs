using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class StudentController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetFavStops(int id)
        {
            try
            {
                List<ApiFavStops> apiFavStops = new List<ApiFavStops>();
                List<FavouriteStop> res = db.FavouriteStops.Where(s => s.student_id == id).ToList();
                if (res.Count > 0)
                {
                    for (int i = 0; i < res.Count; i++)
                    {
                        Stop stop = db.Stops.Find(res[i].stop_id);
                        List<RouteStop> routeStop = db.RouteStops.Where(r => r.stop_id == stop.id).ToList();
                        for (int j = 0; j < routeStop.Count; j++)
                        {
                            ApiFavStops apiFavStop = new ApiFavStops();
                            apiFavStop.Id = Convert.ToInt32(stop.id);
                            apiFavStop.Name = stop.name;
                            apiFavStop.Timing = routeStop[j].stoptiming.ToString();
                            apiFavStop.Latitude = stop.latitude;
                            apiFavStop.Longitude = stop.longitude;
                            apiFavStop.Route = Convert.ToInt32(routeStop[j].route_id);
                            apiFavStops.Add(apiFavStop);
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, apiFavStops);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No Favourite Stops!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpDelete]
        public HttpResponseMessage RemoveFavStop(int studentId, int stopId)
        {
            try
            {
                FavouriteStop favouriteStops = db.FavouriteStops.Where(f => f.student_id == studentId && f.stop_id == stopId).FirstOrDefault();
                if (favouriteStops != null)
                {
                    db.FavouriteStops.Remove(favouriteStops);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Favourite Stop Removed");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No Favourite Stop Found!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage AddFavStop(int studentId, int stopId)
        {
            try
            {
                int favStopCheck = db.FavouriteStops.Where(f => f.student_id == studentId && f.stop_id == stopId).Count();
                if (favStopCheck == 0)
                {
                    FavouriteStop favourite = new FavouriteStop();
                    favourite.stop_id = stopId;
                    favourite.student_id = studentId;
                    db.FavouriteStops.Add(favourite);
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Fav Stop Added");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Fav Stop Already Exist");

                }
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }

        [HttpGet]
        public HttpResponseMessage GetBusesLocations()
        {
            try
            {
                var buses = db.Buses.ToList();
                List<BusLocation> busLocationList = new List<BusLocation>();
                for (int i = 0; i < buses.Count; i++)
                {
                    int busId = Convert.ToInt16(buses[i].id);
                    var locationFromDb = db.TracksLocations.Where(t => t.bus_id == busId && t.date == DateTime.Today).OrderByDescending(t => t.time).FirstOrDefault();
                    if (locationFromDb != null)
                    {
                        BusLocation busLocation = new BusLocation();
                        busLocation.BusId = buses[i].id;
                        busLocation.Cords = new Location
                        {
                            latitude = Convert.ToDouble(locationFromDb.latitude),
                            longitude = Convert.ToDouble(locationFromDb.longitude),
                        };
                        busLocationList.Add(busLocation);
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
