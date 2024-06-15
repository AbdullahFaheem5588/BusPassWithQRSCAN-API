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
                var student = db.Students.Find(id);
                if (student == null)
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Student not found!");
                }
                int studentOrgId = Convert.ToInt32(db.Users.Where(u => u.id == student.user_id).Select(u => u.organization_id).FirstOrDefault());
                var routes = db.Routes.Where(r => r.organization_id == studentOrgId).Select(r => r.id).ToList();
                var sharedRoutes = db.RouteSharings
                    .Where(rs => (rs.organization2_id == studentOrgId && rs.Status == "Accepted"))
                    .Select(rs => rs.route_id).ToList();
                var allRoutes = routes.Cast<int?>().Union(sharedRoutes.Cast<int?>()).ToList();

                List<FavouriteStop> res = db.FavouriteStops.Where(s => s.student_id == id).ToList();
                if (res.Count > 0)
                {
                    foreach (var favStop in res)
                    {
                        Stop stop = db.Stops.Find(favStop.stop_id);
                        List<RouteStop> routeStops = db.RouteStops.Where(rs => rs.stop_id == stop.id && allRoutes.Contains(rs.route_id)).ToList();
                        foreach (var routeStop in routeStops)
                        {
                            ApiFavStops apiFavStop = new ApiFavStops
                            {
                                Id = stop.id,
                                Name = stop.name,
                                Timing = routeStop.stoptiming.ToString(),
                                Latitude = stop.latitude,
                                Longitude = stop.longitude,
                                Route = Convert.ToInt32(routeStop.route_id)
                            };
                            apiFavStops.Add(apiFavStop);
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, apiFavStops);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Favourite Stops!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error: " + ex.Message);
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
    }
}
