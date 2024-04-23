using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class ParentController : ApiController
    {
        BusPassWithQRCodeEntities db = new BusPassWithQRCodeEntities();
        [HttpGet]
        public HttpResponseMessage GetChildren(int id)
        {
            try
            {
                var std = db.Students.Where(s => s.parent_id == id).Select(u => new {
                    u.name,
                    u.gender,
                    u.regno,
                    u.contact,
                    u.qrcode,
                    u.totalJourney,
                    u.remainingJourney,
                    u.passExpiray,
                    u.parent_id,
                    u.user_id,
                }).ToList();
                return Request.CreateResponse(HttpStatusCode.OK, std);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }

        }
        [HttpGet]
        public HttpResponseMessage GetChildTimings(int id)
        {
            try
            {
                List<StudentHistory> history = db.StudentHistories.Where(h => h.student_passid == id && h.date == DateTime.Today).ToList();
                ChildTimings childTimings = new ChildTimings();
                if(history.Count > 0)
                {
                    for (int i = 0; i < history.Count; i++)
                    {
                        if (history[i].type == "pickup_checkin")
                        {
                            childTimings.Pickup_Checkin = history[i].time.ToString();
                        }
                        else if (history[i].type == "pickup_checkout")
                        {
                            childTimings.Pickup_Checkout = history[i].time.ToString();
                        }
                        else if (history[i].type == "dropup_checkin")
                        {
                            childTimings.Dropup_Checkin = history[i].time.ToString();
                        }
                        else if (history[i].type == "dropup_checkout")
                        {
                            childTimings.Dropup_Checkin = history[i].time.ToString();
                        }
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, childTimings);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No Record Found!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Occured!");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetChildLocation(int id)
        {
            try
            {
                Location location = new Location();
                List<StudentHistory> history = db.StudentHistories.Where(h => h.student_passid == id && h.date == DateTime.Today).ToList();
                if (history.Count > 0)
                {
                    if (history.Last().type == "pickup_checkin" || history.Last().type == "dropup_checkin")
                        {
                        List<BusHistory> busHistory = db.BusHistories.Where(b => b.date == DateTime.Today).ToList();
                        List<BusHistory> buses = busHistory.Where(a => a.bus_busno == history.Last().bus_busno).ToList();
                        location.Longitude = buses.Last().longitude;
                        location.Latitude = buses.Last().latitude;
                    }
                    else if (history.Last().type == "pickup_checkout")
                    {
                        location.Longitude = "Uni's Logitude";
                        location.Latitude = "Uni's Latitude";
                    }
                    else if (history.Last().type == "dropup_checkout")
                    {
                        location.Longitude = "At Home";
                        location.Latitude = "At Home";
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, location);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No History");
                }
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error");
            }
        }
    }
}
