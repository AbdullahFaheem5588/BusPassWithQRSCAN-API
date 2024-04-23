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
        BusPassWithQRCodeEntities db = new BusPassWithQRCodeEntities();
        [HttpGet]
        public HttpResponseMessage GetFavStops(int passId)
        {
            try
            {
                List<ApiFavStops> apiFavStops = new List<ApiFavStops>();
                List<FavouriteStop> res = db.FavouriteStops.Where(s => s.student_passid == passId).ToList();
                if(res.Count > 0 )
                {
                    for(int i=0; i<res.Count; i++)
                    {
                        ApiFavStops apiFavStop = new ApiFavStops();
                        Stop stop = db.Stops.Find(res[i].stop_id);
                        List<RouteStop> routeStop = db.RouteStops.Where(r => r.stop_id == stop.id).ToList();
                        for(int j=0; j < routeStop.Count; j++)
                        {
                            apiFavStop.StopNo = Convert.ToInt32(stop.stopno);
                            apiFavStop.Id = Convert.ToInt32(stop.id);
                            apiFavStop.Name = stop.name;
                            apiFavStop.Pickup = stop.pickup.ToString();
                            apiFavStop.Dropup = stop.dropup.ToString();
                            apiFavStop.Latitude = stop.latitude;
                            apiFavStop.Longitude = stop.longitude;
                            apiFavStop.Notification = Convert.ToInt32(res[i].notification);
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
        public HttpResponseMessage RemoveFavStop(int passId, int stopId)
        {
            try
            {
                FavouriteStop favouriteStops = db.FavouriteStops.Where(f => f.student_passid == passId && f.stop_id == stopId).FirstOrDefault();
                if(favouriteStops != null)
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
        public HttpResponseMessage AddFavStop(int studentPassId, int stopId, int Notification)
        {
            try
            {
                FavouriteStop favourite = new FavouriteStop();
                favourite.stop_id = stopId;
                favourite.student_passid = studentPassId;
                favourite.notification = Notification;
                db.FavouriteStops.Add(favourite);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Fav Stop Added");
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
    }
}
