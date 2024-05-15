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
                List<ChildrenWithTimings> childrenWithTimings = new List<ChildrenWithTimings>();
                for(int i = 0; i < childrenFromDB.Count; i++)
                {
                    ApiStudent apiStudent = new ApiStudent
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
                    };
                    List<Travel> history = db.Travels.Where(h => h.student_id == apiStudent.Id && h.date == DateTime.Today).ToList();
                    ChildTimings childTimings = new ChildTimings();
                    for (int j = 0; j < history.Count; j++)
                    {
                        if (history[j].type == "pickup_checkin")
                        {
                            childTimings.Pickup_Checkin = history[j].time.ToString();
                        }
                        else if (history[j].type == "pickup_checkout")
                        {
                            childTimings.Pickup_Checkout = history[j].time.ToString();
                        }
                        else if (history[j].type == "dropoff_checkin")
                        {
                            childTimings.Dropup_Checkin = history[j].time.ToString();
                        }
                        else
                        {
                            childTimings.Dropup_Checkout = history[j].time.ToString();
                        }
                    }
                    childrenWithTimings.Add(new ChildrenWithTimings
                    {
                        childDetails = apiStudent,
                        childTimings = childTimings,
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, childrenWithTimings);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }

        }
        [HttpGet]
        public HttpResponseMessage GetChildLocation(int id)
        {
            try
            {
                List<int> childrenIds = db.Students.Where(p => p.parent_id == id).Select(s => s.id).ToList();
                List<ChildrenLocation> childrenLocation = new List<ChildrenLocation>();
                for(int j=0; j<childrenIds.Count; j++)
                {
                    int studentId = Convert.ToInt32(childrenIds[j]);
                    List<Travel> history = db.Travels.Where(h => h.student_id == studentId && h.date == DateTime.Today).ToList();
                    List<Location> location = new List<Location>();
                    if(history.Count > 0)
                    {
                        if (history.Last().type == "pickup_checkin")
                        {
                            int PickupBusId = Convert.ToInt32(history[0].bus_id);
                            List<TracksLocation> locationsFromDB = db.TracksLocations.Where(h => h.date == DateTime.Today && h.bus_id == PickupBusId).ToList();
                            for (int i = 0; i < locationsFromDB.Count; i++)
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
                        childrenLocation.Add(new ChildrenLocation
                        {
                            Location = location,
                            Name = db.Students.Where(s => s.id == studentId).Select(s => s.name).FirstOrDefault(),
                        });
                    }
                }
                if(childrenLocation.Count > 0)
                    return Request.CreateResponse(HttpStatusCode.OK, childrenLocation);
                else
                    return Request.CreateResponse(HttpStatusCode.NoContent, "No Data Found");
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error");
            }
        }
    }
}
