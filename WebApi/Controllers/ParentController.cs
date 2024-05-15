using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Xml;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class ParentController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetChildren(int id)
        {
            try
            {
                var childrenFromDB = (from s in db.Students
                                      join p in db.Passes on s.pass_id equals p.id
                                      where s.parent_id == id
                                      select new { Student = s, Pass = p }).ToList();
                List<ApiStudent> apiStudent = new List<ApiStudent>();
                for(int i = 0; i < childrenFromDB.Count; i++)
                {
                    apiStudent.Add(new ApiStudent
                    {
                        Id = childrenFromDB[i].Student.id,
                        Name = childrenFromDB[i].Student.name,
                        Gender = childrenFromDB[i].Student.gender,
                        RegNo = childrenFromDB[i].Student.regno,
                        Contact = childrenFromDB[i].Student.contact,
                        ParentId = Convert.ToInt32(childrenFromDB[i].Student.parent_id),
                        UserId = Convert.ToInt32(childrenFromDB[i].Student.user_id),
                        PassId = Convert.ToInt32(childrenFromDB[i].Student.pass_id),
                        PassStatus = childrenFromDB[i].Pass.status,
                        PassExpiry = childrenFromDB[i].Pass.passexpiry.ToString(),
                        TotalJourneys = Convert.ToInt32(childrenFromDB[i].Pass.totaljourneys),
                        RemainingJourneys = Convert.ToInt32(childrenFromDB[i].Pass.remainingjourneys),
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, apiStudent);
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
                List<Travel> history = db.Travels.Where(h => h.student_id == id && h.date == DateTime.Today).ToList();
                ChildTimings childTimings = new ChildTimings();
                if (history.Count > 0)
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
                        else
                        {
                            childTimings.Dropup_Checkout = history[i].time.ToString();
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
                List<Location> location = new List<Location>();
                List<Travel> history = db.Travels.Where(h => h.student_id == id && h.date == DateTime.Today).ToList();
                if (history.Count > 0)
                {
                    if (history.Last().type == "pickup_checkin")
                    {
                        int PickupBusId = Convert.ToInt32(history[0].bus_id);
                        List<TracksLocation> locationsFromDB = db.TracksLocations.Where(h => h.date == DateTime.Today && h.bus_id == PickupBusId).ToList();
                        for (int i = 0;i < locationsFromDB.Count; i++)
                        {
                            location.Add(new Location
                            {
                                latitude = Convert.ToDouble(locationsFromDB[i].latitude),
                                longitude = Convert.ToDouble(locationsFromDB[i].longitude),
                            });
                        }
                    }
                    else if (history.Last().type == "pickup_checkout")
                    {
                        int PickupBusId = Convert.ToInt32(history[0].bus_id);
                        List<TracksLocation> locationsFromDB = db.TracksLocations.Where(h => h.date == DateTime.Today && h.bus_id == PickupBusId).ToList();
                        for (int i = 0; i < locationsFromDB.Count; i++)
                        {
                            if (locationsFromDB[i].time >= history[0].time && locationsFromDB[i].time <= history[1].time)
                                location.Add(new Location
                                {
                                    latitude = Convert.ToDouble(locationsFromDB[i].latitude),
                                    longitude = Convert.ToDouble(locationsFromDB[i].longitude),
                                });
                        }
                    }
                    else if (history.Last().type == "dropoff_checkin")
                    {
                        int PickupBusId = Convert.ToInt32(history[0].bus_id);
                        int DropoffBusId = Convert.ToInt32(history[2].bus_id);
                        List<TracksLocation> locationsFromDB = db.TracksLocations.Where(h => h.date == DateTime.Today && (h.bus_id == PickupBusId || h.bus_id == DropoffBusId)).ToList();
                        for (int i = 0; i < locationsFromDB.Count; i++)
                        {
                            if ((locationsFromDB[i].time >= history[0].time && locationsFromDB[i].time <= history[1].time) || (locationsFromDB[i].time >= history[2].time))
                                location.Add(new Location
                                {
                                    latitude = Convert.ToDouble(locationsFromDB[i].latitude),
                                    longitude = Convert.ToDouble(locationsFromDB[i].longitude),
                                });
                        }
                    }
                    else if (history.Last().type == "dropoff_checkout")
                    {
                        int PickupBusId = Convert.ToInt32(history[0].bus_id);
                        int DropoffBusId = Convert.ToInt32(history[2].bus_id);
                        List<TracksLocation> locationsFromDB = db.TracksLocations.Where(h => h.date == DateTime.Today && (h.bus_id == PickupBusId || h.bus_id == DropoffBusId)).ToList();
                        for (int i = 0; i < locationsFromDB.Count; i++)
                        {
                            if ((locationsFromDB[i].time >= history[0].time && locationsFromDB[i].time <= history[1].time) || (locationsFromDB[i].time >= history[2].time && locationsFromDB[i].time <= history[3].time))
                            location.Add(new Location
                            {
                                latitude = Convert.ToDouble(locationsFromDB[i].latitude),
                                longitude = Convert.ToDouble(locationsFromDB[i].longitude),
                            });
                        }
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
