using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class UsersController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        //[HttpGet]
        //public HttpResponseMessage GetAllUsers()
        //{
        //    List<User> usersFromDb = db.Users.ToList();
        //    var passesFromDb = db.Passes.ToList();

        //    var users = usersFromDb.Select(user => new SingleUser
        //    {
        //        Admins = user.Admins.Select(admin => new ApiAdmin
        //        {
        //            Id = admin.id,
        //            Name = admin.name,
        //            Contact = admin.contact,
        //            Gender = admin.gender,
        //            UserId = Convert.ToInt32(admin.user_id),
        //            UserName = user.username,
        //            Password = user.password,
        //        }).FirstOrDefault(),
        //        Conductors = user.Conductors.Select(conductor => new ApiConductor
        //        {
        //            Id = conductor.id,
        //            Name = conductor.name,
        //            Contact = conductor.contact,
        //            UserId = Convert.ToInt32(conductor.user_id),
        //            UserName = user.username,
        //            Password = user.password,
        //        }).FirstOrDefault(),
        //        Parents = user.Parents.Select(parent => new ApiParent
        //        {
        //            Id = parent.id,
        //            Name = parent.name,
        //            Contact = parent.contact,
        //            UserId = Convert.ToInt32(parent.user_id),
        //            UserName = user.username,
        //            Password = user.password,
        //            ChildrenEnroll = Convert.ToInt32(parent.childrenenroll),
        //        }).FirstOrDefault(),
        //        Students = user.Students.Select(student => new ApiStudent
        //        {
        //            Id = student.id,
        //            Name = student.name,
        //            Contact = student.contact,
        //            Gender = student.gender,
        //            RegNo = student.regno,
        //            ParentId = Convert.ToInt32(student.parent_id),
        //            UserId = Convert.ToInt32(student.user_id),
        //            UserName = user.username,
        //            Password = user.password,
        //            PassId = Convert.ToInt32(student.pass_id),
        //        }).FirstOrDefault(),
        //}).ToList();

        //    var userMapping = new ClassStructures
        //    {
        //        Users = users
        //    };

        //    return Request.CreateResponse(HttpStatusCode.OK, userMapping);
        //}

        [HttpPost]
        public HttpResponseMessage InsertStudent()
        {
            try
            {
                var request = System.Web.HttpContext.Current.Request;
                if (request == null)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Request is null");
                }

                string name = request["Name"];
                string gender = request["Gender"];
                string regNo = request["RegNo"];
                string contact = request["Contact"];
                string parentId = request["ParentId"];
                string password = request["Password"];
                string organizationId = request["OrganizationId"];
                string passExpiry = request["PassExpiry"];
                string totalJourneys = request["TotalJourneys"];

                var imageFile = request.Files["Image"];
                if (imageFile == null || imageFile.ContentLength == 0)
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Image file is missing or empty");
                }

                var path = HttpContext.Current.Server.MapPath("~/Content/Images/" + imageFile.FileName);
                imageFile.SaveAs(path);

                var nextId = ((int)(db.Database.SqlQuery<decimal>("SELECT IDENT_CURRENT('Users')").Single())) + 1;
                var temp = name.Split(' ');
                var userName = temp[0].ToLower() + "." + nextId + "@BPQS.com";
                var existingUser = db.Users.FirstOrDefault(u => u.username == userName);
                if (existingUser == null)
                {
                    User newUser = new User
                    {
                        username = userName,
                        password = password,
                        organization_id = int.Parse(organizationId),
                        role = "Student",
                    };
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    Pass newPass = new Pass
                    {
                        status = "Active",
                        passexpiry = DateTime.Parse(passExpiry),
                        totaljourneys = int.Parse(totalJourneys),
                        remainingjourneys = int.Parse(totalJourneys)
                    };
                    db.Passes.Add(newPass);
                    db.SaveChanges();

                    Student newStudent = new Student
                    {
                        name = name,
                        gender = gender,
                        regno = regNo,
                        contact = contact,
                        parent_id = int.Parse(parentId),
                        user_id = newUser.id,
                        pass_id = newPass.id,
                        image = imageFile.FileName
                    };
                    db.Students.Add(newStudent);
                    db.SaveChanges();
                    int orgId = int.Parse(organizationId);
                    var admins = db.Users.Where(u => u.organization_id == orgId && u.role == "Admin").Select(u => u.id).ToList();
                    for (int i = 0; i < admins.Count; i++)
                    {
                        string description = "Username: " + newUser.username + "\nPassword: " + newUser.password;
                        LocalNotifyUser(admins[i], "New User Added!", description);
                    }

                    return Request.CreateResponse(HttpStatusCode.OK, "Student Inserted Successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Username Already Taken!");
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertAdmin(ApiAdmin admin)
        {
            try
            {
                var nextId = ((int)(db.Database.SqlQuery<decimal>("SELECT IDENT_CURRENT('Users')").Single())) + 1;
                var temp = admin.Name.Split(' ');
                admin.UserName = temp[0].ToLower() + "." + nextId + "@BPQS.com";
                var existingAdmin = db.Users.FirstOrDefault(u => u.username == admin.UserName);
                if (existingAdmin == null)
                {
                    User newUser = new User
                    {
                        username = admin.UserName,
                        password = admin.Password,
                        organization_id = admin.OrganizationId,
                        role = "Admin",
                    };
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    Admin newAdmin = new Admin
                    {
                        name = admin.Name,
                        gender = admin.Gender,
                        contact = admin.Contact,
                        user_id = newUser.id,
                    };
                    db.Admins.Add(newAdmin);
                    db.SaveChanges();
                    var admins = db.Users.Where(u => u.organization_id == admin.OrganizationId && u.role == "Admin").Select(u => u.id).ToList();
                    string discription = "Username: " + newUser.username + "\nPassword: " + newUser.password;
                    for (int i = 0; i < admins.Count; i++)
                    {
                        LocalNotifyUser(admins[i], "New User Added!", discription);
                    }
                    if (admins.Count == 1)
                    {
                        var id = db.Users.Where(u => u.role == "SuperAdmin").Select(u => u.id).FirstOrDefault();
                        LocalNotifyUser(id, "New User Added!", discription);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, "Admin Inserted Successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Username Already Taken!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Admin");
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertParent(ApiParent parent)
        {
            try
            {
                var nextId = ((int)(db.Database.SqlQuery<decimal>("SELECT IDENT_CURRENT('Users')").Single())) + 1;
                var temp = parent.Name.Split(' ');
                parent.UserName = temp[0].ToLower() + "." + nextId + "@BPQS.com";
                var existingParent = db.Users.FirstOrDefault(u => u.username == parent.UserName);
                if (existingParent == null)
                {
                    User newUser = new User
                    {
                        username = parent.UserName,
                        password = parent.Password,
                        organization_id = parent.OrganizationId,
                        role = "Parent",
                    };
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    Parent newParent = new Parent
                    {
                        name = parent.Name,
                        contact = parent.Contact,
                        user_id = newUser.id,
                    };
                    db.Parents.Add(newParent);
                    db.SaveChanges();
                    var admins = db.Users.Where(u => u.organization_id == parent.OrganizationId && u.role == "Admin").Select(u => u.id).ToList();
                    for (int i = 0; i < admins.Count; i++)
                    {
                        string discription = "Username: " + newUser.username + "\nPassword: " + newUser.password;
                        LocalNotifyUser(admins[i], "New User Added!", discription);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, newParent.id);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Username Already Taken!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Parent");
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertConductor(ApiConductor conductor)
        {
            try
            {
                var nextId = ((int)(db.Database.SqlQuery<decimal>("SELECT IDENT_CURRENT('Users')").Single())) + 1;
                var temp = conductor.Name.Split(' ');
                conductor.UserName = temp[0].ToLower() + "." + nextId + "@BPQS.com";
                var existingConductor = db.Users.FirstOrDefault(u => u.username == conductor.UserName);
                if (existingConductor == null)
                {
                    User newUser = new User
                    {
                        username = conductor.UserName,
                        password = conductor.Password,
                        organization_id = conductor.OrganizationId,
                        role = "Conductor",
                    };
                    db.Users.Add(newUser);
                    db.SaveChanges();

                    Conductor newConductor = new Conductor
                    {
                        name = conductor.Name,
                        contact = conductor.Contact,
                        user_id = newUser.id,
                    };
                    db.Conductors.Add(newConductor);
                    db.SaveChanges();
                    var admins = db.Users.Where(u => u.organization_id == conductor.OrganizationId && u.role == "Admin").Select(u => u.id).ToList();
                    for (int i = 0; i < admins.Count; i++)
                    {
                        string discription = "Username: " + newUser.username + "\nPassword: " + newUser.password;
                        LocalNotifyUser(admins[i], "New User Added!", discription);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, newConductor.id);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.Conflict, "Username Already Taken!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Conductor");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetAllParents(int OrganizationId)
        {
            try
            {
                var parents = from p in db.Parents join u in db.Users on p.user_id equals u.id where u.organization_id == OrganizationId select p;
                List<ApiParent> apiParents = new List<ApiParent>();
                foreach (var parent in parents)
                {
                    apiParents.Add(new ApiParent
                    {
                        Id = parent.id,
                        Name = parent.name,
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, apiParents);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }

        }
        [HttpGet]
        public HttpResponseMessage GetAllConductors(int OrganizationId)
        {
            try
            {
                var conductors = from c in db.Conductors join u in db.Users on c.user_id equals u.id where u.organization_id == OrganizationId select c;
                List<ApiConductor> apiConductors = new List<ApiConductor>();
                foreach (var conductor in conductors)
                {
                    apiConductors.Add(new ApiConductor
                    {
                        Id = conductor.id,
                        Name = conductor.name,
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, apiConductors);
            }
            catch
            {

                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }

        }
        [HttpGet]
        public HttpResponseMessage GetUserById(int OrganizationId, int id)
        {
            SingleUser singleUser = new SingleUser();
            User user = db.Users.Where(u => u.id == id && u.organization_id == OrganizationId).FirstOrDefault();
            if (user != null)
            {
                if (user.role == "Admin")
                {
                    var adminDetails = db.Admins.FirstOrDefault(a => a.user_id == user.id);
                    ApiAdmin admin = new ApiAdmin
                    {
                        Name = adminDetails.name,
                        Contact = adminDetails.contact,
                        Gender = adminDetails.gender,
                        Id = adminDetails.id,
                        UserId = user.id,
                        UserName = user.username,
                        Password = user.password,
                    };
                    singleUser.Admins = admin;
                }
                else if (user.role == "Parent")
                {
                    var parentDetails = db.Parents.FirstOrDefault(p => p.user_id == id);
                    ApiParent parent = new ApiParent
                    {
                        Name = parentDetails.name,
                        Contact = parentDetails.contact,
                        ChildrenEnroll = db.Students.Where(s => s.parent_id == parentDetails.id).Count(),
                        Id = parentDetails.id,
                        UserId = user.id,
                        UserName = user.username,
                        Password = user.password,
                    };
                    singleUser.Parents = parent;
                }
                else if (user.role == "Conductor")
                {
                    var conductorDetails = db.Conductors.FirstOrDefault(c => c.user_id == id);
                    ApiConductor conductor = new ApiConductor
                    {
                        Name = conductorDetails.name,
                        Contact = conductorDetails.contact,
                        Id = conductorDetails.id,
                        UserId = user.id,
                        UserName = user.username,
                        Password = user.password,
                    };
                    singleUser.Conductors = conductor;
                }
                else if (user.role == "Student")
                {
                    var studentDetails = db.Students.FirstOrDefault(s => s.user_id == id);
                    ApiStudent student = new ApiStudent
                    {
                        Id = studentDetails.id,
                        Name = studentDetails.name,
                        Contact = studentDetails.contact,
                        PassId = Convert.ToInt32(studentDetails.pass_id),
                        UserId = user.id,
                        Gender = studentDetails.gender,
                        ParentId = Convert.ToInt32(studentDetails.parent_id),
                        Password = user.password,
                        UserName = user.username,
                        RegNo = studentDetails.regno,
                    };
                    var passDetails = db.Passes.FirstOrDefault(p => p.id == student.PassId);
                    student.PassStatus = passDetails.status;
                    student.PassExpiry = passDetails.passexpiry.ToString();
                    student.RemainingJourneys = Convert.ToInt32(passDetails.remainingjourneys);
                    student.TotalJourneys = Convert.ToInt32(passDetails.totaljourneys);

                    singleUser.Students = student;
                }
                return Request.CreateResponse(HttpStatusCode.OK, singleUser);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, "No Such User!");
            }
        }
        [HttpPut]
        public HttpResponseMessage ChangePassword(ApiChangePassword data)
        {
            User user = db.Users.Find(data.id);
            if (user != null)
            {
                if (user.password == data.oldPassword)
                {
                    user.password = data.newPassword;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "Password Changed Successfully");
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Incorrect Old Password!");
                }
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, "No Such User!");
            }
        }
        [HttpDelete]
        public HttpResponseMessage DeleteUser(int OrganizationId, int id)
        {
            User user = db.Users.Where(u => u.id == id && u.organization_id == OrganizationId).FirstOrDefault();
            if (user != null)
            {
                if (user.role == "Student")
                {
                    var student = db.Students.FirstOrDefault(s => s.user_id == id);
                    db.Students.Remove(student);
                    var pass = db.Passes.FirstOrDefault(p => p.id == student.pass_id);
                    db.Passes.Remove(pass);
                }
                else if (user.role == "Parent")
                {
                    var parent = db.Parents.FirstOrDefault(p => p.user_id == id);
                    db.Parents.Remove(parent);
                }
                else if (user.role == "Condcutor")
                {
                    var conductor = db.Conductors.FirstOrDefault(c => c.user_id == id);
                    db.Conductors.Remove(conductor);
                }
                else if (user.role == "Admin")
                {
                    var admin = db.Admins.FirstOrDefault(a => a.user_id == id);
                    db.Admins.Remove(admin);
                }
                db.Users.Remove(user);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "User Deleted");
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, "No Such User!");
            }
        }
        public void updatePassStatus()
        {
            DateTime currentDate = DateTime.Today;
            var passesToUpdate = db.Passes.Where(p => p.passexpiry < currentDate || p.remainingjourneys <= 0).ToList();
            if (passesToUpdate.Count > 0)
            {
                foreach (var pass in passesToUpdate)
                {
                    pass.status = "In-Active";
                    db.SaveChanges();
                }
            }
        }
        [HttpGet]
        public HttpResponseMessage Login(string username, string password)
        {
            updatePassStatus();
            SingleUser singleUser = new SingleUser();
            User user = db.Users.FirstOrDefault(u => u.username == username);
            Organization Organization = new Organization();
            Location OrganizationCords = new Location();
            if (user != null)
            {
                if (user.role != "SuperAdmin")
                {
                    Organization = db.Organizations.Where(o => o.id == user.organization_id).FirstOrDefault();
                    OrganizationCords = new Location
                    {
                        latitude = Convert.ToDouble(Organization.latitude),
                        longitude = Convert.ToDouble(Organization.longitude),
                    };
                }
                if (user.password == password)
                {
                    singleUser.userRole = user.role;
                    if (user.role == "Admin")
                    {
                        var adminDetails = db.Admins.FirstOrDefault(a => a.user_id == user.id);
                        ApiAdmin admin = new ApiAdmin
                        {
                            Name = adminDetails.name,
                            Contact = adminDetails.contact,
                            Gender = adminDetails.gender,
                            Id = adminDetails.id,
                            UserId = user.id,
                            UserName = user.username,
                            Password = user.password,
                            OrganizationId = Convert.ToInt32(user.organization_id),
                            OrganizationCords = OrganizationCords,
                            OrganizationName = Organization.name,
                        };
                        singleUser.Admins = admin;
                    }
                    else if (user.role == "Parent")
                    {
                        var parentDetails = db.Parents.FirstOrDefault(p => p.user_id == user.id);
                        ApiParent parent = new ApiParent
                        {
                            Name = parentDetails.name,
                            Contact = parentDetails.contact,
                            ChildrenEnroll = db.Students.Where(s => s.parent_id == parentDetails.id).Count(),
                            Id = parentDetails.id,
                            UserId = user.id,
                            UserName = user.username,
                            Password = user.password,
                            OrganizationId = Convert.ToInt32(user.organization_id),
                            OrganizationCords = OrganizationCords,
                            OrganizationName = Organization.name,
                        };
                        singleUser.Parents = parent;
                    }
                    else if (user.role == "Conductor")
                    {
                        var conductorDetails = db.Conductors.FirstOrDefault(c => c.user_id == user.id);
                        var busDetails = db.Buses.FirstOrDefault(b => b.conductor_id == conductorDetails.id);
                        ApiConductor conductor = new ApiConductor
                        {
                            Name = conductorDetails.name,
                            Contact = conductorDetails.contact,
                            Id = conductorDetails.id,
                            UserId = user.id,
                            UserName = user.username,
                            Password = user.password,
                            BusId = busDetails.id,
                            BusRegNo = busDetails.regno,
                            TotalSeats = Convert.ToInt32(busDetails.totalSeats),
                            OrganizationId = Convert.ToInt32(user.organization_id),
                            OrganizationCords = OrganizationCords,
                            OrganizationName = Organization.name,
                        };
                        singleUser.Conductors = conductor;
                    }
                    else if (user.role == "Student")
                    {
                        var studentDetails = db.Students.FirstOrDefault(s => s.user_id == user.id);
                        ApiStudent student = new ApiStudent
                        {
                            Id = studentDetails.id,
                            Name = studentDetails.name,
                            Contact = studentDetails.contact,
                            PassId = Convert.ToInt32(studentDetails.pass_id),
                            UserId = user.id,
                            Gender = studentDetails.gender,
                            ParentId = Convert.ToInt32(studentDetails.parent_id),
                            Password = user.password,
                            UserName = user.username,
                            RegNo = studentDetails.regno,
                            OrganizationId = Convert.ToInt32(user.organization_id),
                            OrganizationCords = OrganizationCords,
                            OrganizationName = Organization.name,
                        };
                        var passDetails = db.Passes.FirstOrDefault(p => p.id == student.PassId);
                        student.PassStatus = passDetails.status;
                        student.PassExpiry = passDetails.passexpiry.ToString();
                        student.RemainingJourneys = Convert.ToInt32(passDetails.remainingjourneys);
                        student.TotalJourneys = Convert.ToInt32(passDetails.totaljourneys);

                        singleUser.Students = student;
                    }
                    else if (user.role == "SuperAdmin")
                    {
                        ApiSuperAdmin superAdmin = new ApiSuperAdmin
                        {
                            UserName = user.username,
                            Password = user.password,
                            UserId = user.id,
                        };
                        singleUser.SuperAdmin = superAdmin;
                    }
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Incorrect Password!");
                }
                return Request.CreateResponse(HttpStatusCode.OK, singleUser);
            }
            else
            {
                return Request.CreateResponse(HttpStatusCode.OK, "Incorrect User Name");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetUserNotification(int id)
        {
            try
            {
                List<Notification> notifications = db.Notifications.Where(n => n.user_id == id).OrderByDescending(n => n.id).ToList();
                List<ApiNotification> apiNotifications = new List<ApiNotification>();
                if (notifications.Count > 0)
                {
                    for (int i = 0; i < notifications.Count; i++)
                    {
                        apiNotifications.Add(new ApiNotification
                        {
                            Id = notifications[i].id,
                            Date = notifications[i].date.ToString(),
                            Description = notifications[i].description,
                            Time = notifications[i].time.ToString(),
                            Type = notifications[i].type,
                            UserID = Convert.ToInt16(notifications[i].user_id),
                            NotificationRead = Convert.ToInt16(notifications[i].notificationRead),
                        });
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, apiNotifications);
                }
                else
                    return Request.CreateResponse(HttpStatusCode.OK, "No Record Found");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error");
            }
        }
        public void LocalNotifyUser(int userId, string Type, string Description)
        {
            Notification notification = new Notification();
            notification.date = DateTime.Today;
            notification.time = DateTime.Now.TimeOfDay;
            notification.type = Type;
            notification.description = Description;
            notification.user_id = userId;
            notification.notificationRead = 0;
            db.Notifications.Add(notification);
            db.SaveChanges();
        }
        [HttpPost]
        public HttpResponseMessage NotifyUser(int userId, string Type, string Description)
        {
            try
            {
                LocalNotifyUser(userId, Type, Description);
                return Request.CreateResponse(HttpStatusCode.OK, "User Notified");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage MakeAnnouncement(int OrganizationId, string Description)
        {
            try
            {
                List<User> user = db.Users.Where(u => u.organization_id == OrganizationId).ToList();
                for (int i = 0; i < user.Count; i++)
                {
                    Notification notification = new Notification();
                    notification.date = DateTime.Today;
                    notification.time = DateTime.Now.TimeOfDay;
                    notification.type = "Announcement!";
                    notification.description = Description;
                    notification.user_id = user[i].id;
                    notification.notificationRead = 0;
                    db.Notifications.Add(notification);
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK, "Announcement Made Succesfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetUserHistory(int id, string fDate, string tDate)
        {
            DateTime fromDate = Convert.ToDateTime(fDate);
            DateTime toDate = Convert.ToDateTime(tDate);
            try
            {
                string userRole = db.Users.FirstOrDefault(u => u.id == id)?.role;
                if (userRole == "Student")
                {
                    var travelFromDB = db.Travels.Join(db.Students, t => t.student_id, s => s.id, (t, s) => new { Travel = t, Student = s })
                    .Join(db.Users, ts => ts.Student.user_id, u => u.id, (ts, u) => new { Travel = ts.Travel, User = u })
                    .Where(ut => ut.User.id == id && ut.Travel.date >= fromDate && ut.Travel.date <= toDate)
                    .Select(ut => ut.Travel).ToList();
                    List<ApiTravel> apiTravel = new List<ApiTravel>();
                    for (int i = 0; i < travelFromDB.Count; i++)
                    {
                        apiTravel.Add(new ApiTravel
                        {
                            Id = travelFromDB[i].id,
                            Date = travelFromDB[i].date.ToString(),
                            Time = travelFromDB[i].time.ToString(),
                            Type = travelFromDB[i].type.ToString(),
                            PassId = Convert.ToInt32(travelFromDB[i].pass_id),
                            StudentId = Convert.ToInt32(travelFromDB[i].student_id),
                            BusId = Convert.ToInt32(travelFromDB[i].bus_id),
                            RouteId = Convert.ToInt32(travelFromDB[i].route_id),
                            StopId = Convert.ToInt32(travelFromDB[i].stop_id),
                        });
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, apiTravel);
                }
                else if (userRole == "Parent")
                {
                    var childIdsFromDB = db.Students
                        .Join(db.Users, s => s.parent_id, u => u.id, (s, u) => new { Student = s, User = u })
                        .Where(su => su.User.id == id).Select(su => su.Student.id).ToList();
                    List<List<ApiTravel>> apiTravel = new List<List<ApiTravel>>();
                    for (int i = 0; i < childIdsFromDB.Count; i++)
                    {
                        var childId = Convert.ToInt32(childIdsFromDB[i]);
                        List<ApiTravel> singleChildTravel = new List<ApiTravel>();
                        var travelFromDB = db.Travels
                            .Join(db.Students, t => t.student_id, s => s.id, (t, s) => new { Travel = t, Student = s })
                            .Where(ut => ut.Student.id == childId && ut.Travel.date >= fromDate && ut.Travel.date <= toDate)
                            .Select(ut => ut.Travel).ToList();
                        for (int j = 0; j < travelFromDB.Count; j++)
                        {
                            int studentId = Convert.ToInt32(travelFromDB[j].student_id);
                            singleChildTravel.Add(new ApiTravel
                            {
                                Id = travelFromDB[j].id,
                                Date = travelFromDB[j].date.ToString(),
                                Time = travelFromDB[j].time.ToString(),
                                Type = travelFromDB[j].type.ToString(),
                                PassId = Convert.ToInt32(travelFromDB[j].pass_id),
                                StudentId = studentId,
                                BusId = Convert.ToInt32(travelFromDB[j].bus_id),
                                RouteId = Convert.ToInt32(travelFromDB[j].route_id),
                                StopId = Convert.ToInt32(travelFromDB[j].stop_id),
                                StudentName = db.Students.Where(s => s.id == studentId).Select(s => s.name).FirstOrDefault()
                            });
                        }
                        apiTravel.Add(singleChildTravel);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, apiTravel);
                }
                else if (userRole == "Conductor")
                {
                    List<BusHistory> busHistory = new List<BusHistory>();
                    var travelFromDB = db.Travels.Join(db.Buses, t => t.bus_id, b => b.id, (t, b) => new { Travel = t, Bus = b })
                    .Join(db.Conductors, tb => tb.Bus.conductor_id, c => c.id, (tb, c) => new { Travel = tb.Travel, Conductor = c })
                    .Join(db.Users, tbc => tbc.Conductor.user_id, u => u.id, (tbc, u) => new { Travel = tbc.Travel, User = u })
                    .Where(tu => tu.User.id == id && tu.Travel.date >= fromDate && tu.Travel.date <= toDate)
                    .Select(tu => tu.Travel).ToList();

                    var startsFromDB = db.Starts
                        .Join(db.Buses, s => s.bus_id, b => b.id, (s, b) => new { Start = s, Bus = b })
                        .Join(db.Conductors, sb => sb.Bus.conductor_id, c => c.id, (sb, c) => new { Start = sb.Start, Conductor = c })
                        .Join(db.Users, sbc => sbc.Conductor.user_id, u => u.id, (sbc, u) => new { Start = sbc.Start, User = u })
                        .Where(su => su.User.id == id && su.Start.date >= fromDate && su.Start.date <= toDate)
                        .Select(su => su.Start)
                        .ToList();

                    var reachesFromDB = db.Reaches
                        .Join(db.Buses, s => s.bus_id, b => b.id, (s, b) => new { Start = s, Bus = b })
                        .Join(db.Conductors, sb => sb.Bus.conductor_id, c => c.id, (sb, c) => new { Start = sb.Start, Conductor = c })
                        .Join(db.Users, sbc => sbc.Conductor.user_id, u => u.id, (sbc, u) => new { Start = sbc.Start, User = u })
                        .Where(su => su.User.id == id && su.Start.date >= fromDate && su.Start.date <= toDate)
                        .Select(su => su.Start)
                        .ToList();
                    for (int i = 0; i < travelFromDB.Count; i++)
                    {
                        int stopId = Convert.ToInt32(travelFromDB[i].stop_id);
                        string stopName = db.Stops.Where(s => s.id == stopId).Select(s => s.name).FirstOrDefault();
                        if (stopId == 0)
                            stopName = "BIIT";

                        busHistory.Add(new BusHistory
                        {
                            Date = travelFromDB[i].date.ToString(),
                            Time = travelFromDB[i].time.ToString(),
                            Type = travelFromDB[i].type.ToString(),
                            Description = "Bus No: " + travelFromDB[i].bus_id.ToString() + "\nRoute No: " + travelFromDB[i].route_id.ToString()
                            + "\nStop Name: " + stopName + "\nPass No: " + travelFromDB[i].pass_id.ToString(),
                        });
                    }
                    for (int i = 0; i < startsFromDB.Count; i++)
                    {
                        busHistory.Add(new BusHistory
                        {
                            Date = startsFromDB[i].date.ToString(),
                            Time = startsFromDB[i].time.ToString(),
                            Type = "Journey Started",
                            Description = "Bus No: " + startsFromDB[i].bus_id.ToString()
                                          + "\nRoute No: " + startsFromDB[i].route_id.ToString(),
                        });
                    }
                    for (int i = 0; i < reachesFromDB.Count; i++)
                    {
                        int stopId = Convert.ToInt32(reachesFromDB[i].stop_id);
                        string stopName = db.Stops.Where(s => s.id == stopId).Select(s => s.name).FirstOrDefault();
                        if (stopId == 0)
                            stopName = "BIIT";
                        busHistory.Add(new BusHistory
                        {
                            Date = reachesFromDB[i].date.ToString(),
                            Time = reachesFromDB[i].time.ToString(),
                            Type = "Reached at Stop",
                            Description = "Bus No: " + reachesFromDB[i].bus_id.ToString() + "\nRoute No: " + reachesFromDB[i].route_id.ToString()
                                        + "\nStop Name: " + stopName,
                        });
                    }
                    var orderedBusHistory = busHistory.OrderBy(bh => DateTime.Parse(bh.Date))
                                      .ThenBy(bh => TimeSpan.Parse(bh.Time))
                                      .ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, orderedBusHistory);
                }
                else if (userRole == "Admin")
                {
                    int organizationId = Convert.ToInt32(db.Users.Where(u => u.id == id).Select(u => u.organization_id).FirstOrDefault());
                    List<BusHistory> busHistory = new List<BusHistory>();
                    var travelFromDB = db.Travels.Join(db.Students, t => t.student_id, s => s.id, (t, s) => new { Travel = t, Student = s })
                   .Join(db.Users, ts => ts.Student.user_id, u => u.id, (ts, u) => new { ts.Travel, User = u })
                   .Where(joined => joined.User.organization_id == 1 && joined.Travel.date >= fromDate && joined.Travel.date <= toDate)
                   .Select(joined => joined.Travel).ToList();

                    var startsFromDB = db.Starts.Join(db.Buses, s => s.bus_id, b => b.id, (s, b) => new { Starts = s, Buses = b })
                        .Where(joined => joined.Starts.date >= fromDate && joined.Starts.date <= toDate &&
                        joined.Buses.organization_id == organizationId).Select(joined => joined.Starts).ToList();

                    var reachesFromDB = db.Reaches.Join(db.Buses, r => r.bus_id, b => b.id, (r, b) => new { Reaches = r, Buses = b })
                        .Where(joined => joined.Reaches.date >= fromDate && joined.Reaches.date <= toDate &&
                        joined.Buses.organization_id == organizationId).Select(joined => joined.Reaches).ToList();
                    List<ApiTravel> apiTravel = new List<ApiTravel>();
                    List<ApiStart> apiStart = new List<ApiStart>();
                    List<ApiReach> apiReaches = new List<ApiReach>();
                    for (int i = 0; i < travelFromDB.Count; i++)
                    {
                        int stopId = Convert.ToInt32(travelFromDB[i].stop_id);
                        string stopName = db.Stops.Where(s => s.id == stopId).Select(s => s.name).FirstOrDefault();
                        if (stopId == 0)
                            stopName = "BIIT";

                        busHistory.Add(new BusHistory
                        {
                            Date = travelFromDB[i].date.ToString(),
                            Time = travelFromDB[i].time.ToString(),
                            Type = travelFromDB[i].type.ToString(),
                            Description = "Bus No: " + travelFromDB[i].bus_id.ToString() + "\nRoute No: " + travelFromDB[i].route_id.ToString()
                            + "\nStop Name: " + stopName + "\nPass No: " + travelFromDB[i].pass_id.ToString(),
                        });
                    }
                    for (int i = 0; i < startsFromDB.Count; i++)
                    {
                        busHistory.Add(new BusHistory
                        {
                            Date = startsFromDB[i].date.ToString(),
                            Time = startsFromDB[i].time.ToString(),
                            Type = "Journey Started",
                            Description = "Bus No: " + startsFromDB[i].bus_id.ToString()
                                          + "\nRoute No: " + startsFromDB[i].route_id.ToString(),
                        });
                    }
                    for (int i = 0; i < reachesFromDB.Count; i++)
                    {
                        int stopId = Convert.ToInt32(reachesFromDB[i].stop_id);
                        string stopName = db.Stops.Where(s => s.id == stopId).Select(s => s.name).FirstOrDefault();
                        if (stopId == 0)
                            stopName = "BIIT";
                        busHistory.Add(new BusHistory
                        {
                            Date = reachesFromDB[i].date.ToString(),
                            Time = reachesFromDB[i].time.ToString(),
                            Type = "Reached at Stop",
                            Description = "Bus No: " + reachesFromDB[i].bus_id.ToString() + "\nRoute No: " + reachesFromDB[i].route_id.ToString()
                                        + "\nStop Name: " + stopName,
                        });
                    }
                    var orderedBusHistory = busHistory.OrderBy(bh => DateTime.Parse(bh.Date))
                                      .ThenBy(bh => TimeSpan.Parse(bh.Time))
                                      .ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, orderedBusHistory);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.OK, "Invalid User Id!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPut]
        public HttpResponseMessage MarkNotificationAsRead(int id)
        {
            try
            {
                var notification = db.Notifications.Find(id);
                notification.notificationRead = 1;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Notification Marked as Read!");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPut]
        public HttpResponseMessage MarkAllNotificationAsRead(int userId)
        {
            try
            {
                var notifications = db.Notifications.Where(n => n.user_id == userId).ToList();
                for (int i = 0; i < notifications.Count; i++)
                {
                    notifications[i].notificationRead = 1;
                }
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Notifications Marked as Read!");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
    }
}