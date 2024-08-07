﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class AdminController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetBusDetails(int OrganizationId)
        {
            try
            {
                var busDetailsList = new List<object>();

                var buses = db.Buses.Where(b => b.organization_id == OrganizationId).ToList();

                foreach (var bus in buses)
                {
                    var busId = bus.id;
                    var totalSeats = bus.totalSeats;

                    var startCount = (from s in db.Starts
                                      where s.bus_id == busId && s.date == DateTime.Today
                                      select s).Count();

                    int bookedSeats = 0;

                    if (startCount > 0)
                    {
                        bookedSeats = ((from t in db.Travels
                                        where t.bus_id == busId && t.date == DateTime.Today
                                        && (t.type.Contains("pickup_checkin") || t.type.Contains("dropoff_checkin"))
                                        select t).Count()) - ((from t in db.Travels
                                                               where t.bus_id == busId && t.date == DateTime.Today
                                                               && (t.type.Contains("pickup_checkout") || t.type.Contains("dropoff_checkout"))
                                                               select t).Count());
                    }

                    int totalStops = 0;
                    int remainingStops = 0;

                    var starts = db.Starts.Where(s => s.date == DateTime.Today && s.bus_id == busId)
                                          .OrderByDescending(s => s.id).FirstOrDefault();

                    if (starts != null)
                    {
                        JourneyStopsChecker journeyStopsChecker = new JourneyStopsChecker();
                        totalStops = db.RouteStops.Where(rs => rs.route_id == starts.route_id).Count();
                        var time = DateTime.Now.TimeOfDay;
                        remainingStops = totalStops - db.Reaches
                            .Where(r => r.date == DateTime.Today && time >= r.time && r.route_id == starts.route_id && r.stop_id != null)
                            .Count();
                    }

                    busDetailsList.Add(new
                    {
                        BusId = busId,
                        TotalSeats = totalSeats,
                        BookedSeats = bookedSeats,
                        TotalStops = totalStops,
                        RemainingStops = remainingStops
                    });
                }

                return Request.CreateResponse(HttpStatusCode.OK, busDetailsList);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertStop(Stop stop)
        {
            try
            {
                db.Stops.Add(stop);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "New Stop Added Successfully");

            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Stop");
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertRoute(Routes route)
        {
            try
            {
                Route newRoute = new Route
                {
                    Title = route.RouteTitle,
                    organization_id = route.OrganizationId,
                };
                db.Routes.Add(newRoute);
                for (int i = 0; i < route.Stops.Count; i++)
                {
                    db.RouteStops.Add(new RouteStop
                    {
                        route_id = newRoute.id,
                        stop_id = route.Stops[i].Id,
                        stoptiming = null,
                    });
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "New Route Added Successfully");

            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Route");
            }
        }
        [HttpPut]
        public HttpResponseMessage RechargeJourneys(int passId, int noOfJourneys, DateTime passExpiry)
        {
            try
            {
                var passDetails = db.Passes.Find(passId);
                if (passDetails != null)
                {
                    if (passDetails.remainingjourneys == 0)
                        passDetails.totaljourneys = noOfJourneys;
                    else
                        passDetails.totaljourneys += noOfJourneys;
                    passDetails.remainingjourneys += noOfJourneys;
                    passDetails.status = "Active";
                    passDetails.passexpiry = passExpiry;
                    db.SaveChanges();
                    var student = db.Students.Where(s => s.pass_id == passId).FirstOrDefault();
                    var parentUserId = db.Parents.Where(p => p.id == student.parent_id).Select(p => p.user_id).FirstOrDefault();
                    UsersController usersController = new UsersController();
                    usersController.LocalNotifyUser(Convert.ToInt32(student.user_id), "Pass Recharged!", "Your Pass has been recharged with " + noOfJourneys + " more journeys");
                    usersController.LocalNotifyUser(Convert.ToInt32(parentUserId), "Pass Recharged!", student.name + "'s Pass has been recharged with " + noOfJourneys + " more journeys");
                    return Request.CreateResponse(HttpStatusCode.OK, "Pass Recharged");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Pass Id");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage ValidatePass(int passId, int adminOrganizationId)
        {
            try
            {
                var studentOrganizationId = (from u in db.Users join s in db.Students on u.id equals s.user_id where s.pass_id == passId select u.organization_id).FirstOrDefault();
                var passDetails = db.Passes.Find(passId);
                if (passDetails != null && studentOrganizationId == adminOrganizationId)
                {
                    return Request.CreateResponse(HttpStatusCode.OK, passDetails.status);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertBus(BusDetails busDetails)
        {
            try
            {
                Bus bus = new Bus
                {
                    regno = busDetails.RegNo,
                    totalSeats = busDetails.TotalSeats,
                    conductor_id = busDetails.Conductor.Id,
                    organization_id = busDetails.OrganizationId,
                };
                db.Buses.Add(bus);
                for (int i = 0; i < busDetails.Routes.Count; i++)
                {
                    db.IsAssigneds.Add(new IsAssigned
                    {
                        bus_id = bus.id,
                        route_id = busDetails.Routes[i].RouteId,
                    });
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "New Bus Added Successfully");

            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Route");
            }
        }
        [HttpGet]
        public HttpResponseMessage Search(int id, string category, int OrganizationId)
        {
            try
            {
                if (category == "Student")
                {
                    var studentDetails = (from s in db.Students join u in db.Users on s.user_id equals u.id where (s.pass_id == id && u.organization_id == OrganizationId) select s).FirstOrDefault();
                    if (studentDetails != null)
                    {
                        ApiStudent apiStudent = new ApiStudent
                        {
                            Id = studentDetails.id,
                            PassId = Convert.ToInt32(studentDetails.pass_id),
                            Name = studentDetails.name,
                            RegNo = studentDetails.regno,
                            Contact = studentDetails.contact,
                            PassStatus = db.Passes.Where(p => p.id == id).Select(p => p.status).FirstOrDefault(),
                        };
                        return Request.CreateResponse(HttpStatusCode.OK, apiStudent);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No such Student Found!");
                    }
                }
                else if (category == "Parent")
                {
                    var parentDetails = (from p in db.Parents join u in db.Users on p.user_id equals u.id where (p.id == id && u.organization_id == OrganizationId) select p).FirstOrDefault();
                    if (parentDetails != null)
                    {
                        ApiParent apiParent = new ApiParent { Id = parentDetails.id, Name = parentDetails.name, Contact = parentDetails.contact };
                        return Request.CreateResponse(HttpStatusCode.OK, apiParent);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No such Parent Found!");
                    }
                }
                else if (category == "Conductor")
                {
                    var conductorDetails = (from c in db.Conductors join u in db.Users on c.user_id equals u.id where (c.id == id && u.organization_id == OrganizationId) select c).FirstOrDefault();
                    if (conductorDetails != null)
                    {
                        ApiConductor apiConductor = new ApiConductor { Id = conductorDetails.id, Name = conductorDetails.name, Contact = conductorDetails.contact };
                        return Request.CreateResponse(HttpStatusCode.OK, apiConductor);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No such Conductor Found!");
                    }
                }
                else if (category == "Bus")
                {
                    var bus = db.Buses.Where(b => b.id == id && b.organization_id == OrganizationId).FirstOrDefault();
                    if (bus != null)
                    {
                        var conductor = db.Conductors.Find(bus.conductor_id);
                        var conductorDetails = new ApiConductor
                        {
                            Id = conductor.id,
                            Name = conductor.name,
                        };
                        BusDetails busDetails = new BusDetails
                        {
                            Id = bus.id,
                            RegNo = bus.regno,
                            TotalSeats = Convert.ToInt32(bus.totalSeats),
                            Conductor = conductorDetails,
                        };
                        var routes = (from r in db.Routes
                                      join ia in db.IsAssigneds on r.id equals ia.route_id
                                      where ia.bus_id == id
                                      select r).ToList();
                        var routeDetails = new List<Routes>();
                        for (int i = 0; i < routes.Count; i++)
                        {
                            routeDetails.Add(new Routes
                            {
                                RouteId = routes[i].id,
                                RouteTitle = routes[i].Title,
                            });
                        }
                        busDetails.Routes = routeDetails;
                        return Request.CreateResponse(HttpStatusCode.OK, busDetails);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No such Bus Found!");
                    }
                }
                else if (category == "Stop")
                {
                    var stop = db.Stops.Find(id);
                    if (stop != null)
                    {
                        ApiStops apiStops = new ApiStops
                        {
                            Id = stop.id,
                            Name = stop.name,
                        };
                        return Request.CreateResponse(HttpStatusCode.OK, apiStops);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No such Stop Found!");
                    }
                }
                else if (category == "Route")
                {
                    var route = db.Routes.Where(r => r.id == id && r.organization_id == OrganizationId).FirstOrDefault();
                    if (route != null)
                    {
                        var stops = (from s in db.Stops
                                     join rs in db.RouteStops on s.id equals rs.stop_id
                                     where rs.route_id == id
                                     select s).ToList();
                        var stopDetails = new List<ApiStops>();
                        for (int i = 0; i < stops.Count; i++)
                        {
                            stopDetails.Add(new ApiStops
                            {
                                Id = stops[i].id,
                                Name = stops[i].name,
                            });
                        }
                        var routeDetails = new Routes
                        {
                            RouteId = route.id,
                            RouteTitle = route.Title,
                            Stops = stopDetails,
                        };
                        return Request.CreateResponse(HttpStatusCode.OK, routeDetails);
                    }
                    else
                    {
                        return Request.CreateResponse(HttpStatusCode.NotFound, "No such Stop Found!");
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "Invalid Searching Category!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateStudent(ApiStudent apiStudent)
        {
            try
            {
                var studentDetails = db.Students.Where(s => s.pass_id == apiStudent.PassId).FirstOrDefault();
                var passDetails = db.Passes.Find(apiStudent.PassId);
                if (studentDetails != null && passDetails != null)
                {
                    studentDetails.name = apiStudent.Name;
                    studentDetails.regno = apiStudent.RegNo;
                    studentDetails.contact = apiStudent.Contact;
                    passDetails.status = apiStudent.PassStatus;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Student Details Updated!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Student Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateParent(ApiParent apiParent)
        {
            try
            {
                var parentDetails = db.Parents.Find(apiParent.Id);
                if (parentDetails != null)
                {
                    parentDetails.name = apiParent.Name;
                    parentDetails.contact = apiParent.Contact;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Parent Details Updated!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Parent Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateConductor(ApiConductor apiConductor)
        {
            try
            {
                var conductorDetails = db.Conductors.Find(apiConductor.Id);
                if (conductorDetails != null)
                {
                    conductorDetails.name = apiConductor.Name;
                    conductorDetails.contact = apiConductor.Contact;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Conductor Details Updated!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Conductor Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateBus(BusDetails busDetails)
        {
            try
            {
                var bus = db.Buses.Find(busDetails.Id);
                if (bus != null)
                {
                    bus.regno = busDetails.RegNo;
                    bus.totalSeats = busDetails.TotalSeats;
                    bus.conductor_id = busDetails.Conductor.Id;

                    var recordsToDelete = db.IsAssigneds.Where(ia => ia.bus_id == busDetails.Id).ToList();
                    db.IsAssigneds.RemoveRange(recordsToDelete);

                    for (int i = 0; i < busDetails.Routes.Count; i++)
                    {
                        db.IsAssigneds.Add(new IsAssigned
                        {
                            bus_id = busDetails.Id,
                            route_id = busDetails.Routes[i].RouteId,
                        });
                    }
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Bus Details Updated!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Bus Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateStop(ApiStops apiStops)
        {
            try
            {
                var stop = db.Stops.Find(apiStops.Id);
                if (stop != null)
                {
                    stop.name = apiStops.Name;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Stop Details Updated!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Stop Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateRoute(Routes apiRoute)
        {
            try
            {
                var route = db.Routes.Find(apiRoute.RouteId);
                if (route != null)
                {
                    route.Title = apiRoute.RouteTitle;

                    var recordsToDelete = db.RouteStops.Where(rs => rs.route_id == apiRoute.RouteId).ToList();
                    db.RouteStops.RemoveRange(recordsToDelete);

                    for (int i = 0; i < apiRoute.Stops.Count; i++)
                    {
                        db.RouteStops.Add(new RouteStop
                        {
                            route_id = apiRoute.RouteId,
                            stop_id = apiRoute.Stops[i].Id,
                        });
                    }
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Route Details Updated!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Such Route Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetSharedRoutesRecord(int OrganizationId)
        {
            try
            {
                var sharedRoutes = db.RouteSharings.Where(rs => rs.organization1_id == OrganizationId || rs.organization2_id == OrganizationId).ToList();
                List<ApiSharedRoutes> apiSharedRoutes = new List<ApiSharedRoutes>();
                for (int i = 0; i < sharedRoutes.Count; i++)
                {
                    int org1_Id = Convert.ToInt16(sharedRoutes[i].organization1_id);
                    int org2_Id = Convert.ToInt16(sharedRoutes[i].organization2_id);
                    int route_Id = Convert.ToInt16(sharedRoutes[i].route_id);
                    string ownedBy = db.Organizations.Where(o => o.id == org1_Id).Select(o => o.name).FirstOrDefault();
                    string requestedBy = db.Organizations.Where(o => o.id == org2_Id).Select(o => o.name).FirstOrDefault();
                    bool requestedByUser = true;
                    if (OrganizationId == org1_Id)
                    {
                        requestedByUser = false;
                    }
                    apiSharedRoutes.Add(new ApiSharedRoutes
                    {
                        Id = sharedRoutes[i].id,
                        RouteTitle = db.Routes.Where(r => r.id == route_Id).Select(r => r.Title).FirstOrDefault(),
                        Status = sharedRoutes[i].Status,
                        RequestedByUser = requestedByUser,
                        Description = "Owned By: " + ownedBy + "\nRequested By: " + requestedBy + "\nRouteNo: " + route_Id + "\nTotal Stops: " + db.RouteStops.Where(rs => rs.route_id == route_Id).Count(),
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, apiSharedRoutes);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPut]
        public HttpResponseMessage UpdateRequestStatus(int Id, string Status)
        {
            try
            {
                var request = db.RouteSharings.Find(Id);
                if (request != null)
                {
                    request.Status = Status;
                    db.SaveChanges();
                    var admins = (from a in db.Admins join u in db.Users on a.user_id equals u.id where u.organization_id == request.organization2_id select a.user_id).ToList();
                    UsersController usersController = new UsersController();
                    for (int i = 0; i < admins.Count; i++)
                    {
                        usersController.LocalNotifyUser(Convert.ToInt32(admins[i]), "Route Sharing Request!", "Your Route Sharing Request on Route # " + request.route_id + " has been " + Status);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "Request " + Status + "!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No Such Request Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage SendRouteSharingRequest(int RouteId, int OrganizationId)
        {
            try
            {
                var route = db.Routes.Find(RouteId);
                if (route != null)
                {
                    RouteSharing routeSharing = new RouteSharing
                    {
                        organization1_id = route.organization_id,
                        organization2_id = OrganizationId,
                        route_id = route.id,
                        Status = "Pending",
                    };
                    db.RouteSharings.Add(routeSharing);
                    db.SaveChanges();
                    var admins = (from a in db.Admins join u in db.Users on a.user_id equals u.id where u.organization_id == route.organization_id select a.user_id).ToList();
                    UsersController usersController = new UsersController();
                    for (int i = 0; i < admins.Count; i++)
                    {
                        usersController.LocalNotifyUser(Convert.ToInt32(admins[i]), "Route Sharing Request!", db.Organizations.Where(o => o.id == OrganizationId).Select(o => o.name).FirstOrDefault() + " has requested you to Share your Route No: " + route.id + " with them.");
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "Request Sent!");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "No Such Route Found!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpGet]
        public HttpResponseMessage GetRouteRaking(int OrganizationId)
        {
            try
            {
                var routes = db.Routes.Where(r => r.organization_id == OrganizationId).ToList();
                List<RouteRaking> routeRakings = new List<RouteRaking>();
                for (int i = 0; i < routes.Count; i++)
                {
                    RouteRaking routeRaking = new RouteRaking();
                    routeRaking.RouteId = Convert.ToInt32(routes[i].id);
                    var travels = db.Travels.Where(t => t.date == DateTime.Today && t.route_id == routeRaking.RouteId && (t.type == "pickup_checkin" || t.type == "dropoff_checkin")).FirstOrDefault();
                    if (travels != null)
                    {
                        routeRaking.BusId = Convert.ToInt32(travels.bus_id);
                        if (travels.type == "pickup_checkin")
                            routeRaking.TravelType = "Pickup";
                        else
                            routeRaking.TravelType = "Dropoff";
                        string type = travels.type;
                        routeRaking.MaxPassengers = Convert.ToInt32(db.Travels.Where(t => t.date == DateTime.Today && t.route_id == routeRaking.RouteId && t.bus_id == routeRaking.BusId && t.type == type).Count());
                        routeRakings.Add(routeRaking);
                    }
                }
                routeRakings.OrderByDescending(rs => rs.MaxPassengers);
                return Request.CreateResponse(HttpStatusCode.OK, routeRakings);
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
    }
}
