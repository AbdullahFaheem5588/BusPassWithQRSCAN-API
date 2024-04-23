using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class UsersController : ApiController
    {
        BusPassWithQRCodeEntities db = new BusPassWithQRCodeEntities();
        [HttpGet]
        public HttpResponseMessage GetAllUsers()
        {
            List<User> usersFromDb = db.Users.ToList();

            var users = usersFromDb.Select(user => new SingleUser
            {
                Admins = user.Admins.Select(admin => new ApiAdmin
                {
                    Id = admin.id,
                    Name = admin.name,
                    Contact = admin.contact,
                    Gender = admin.gender,
                    UserId = Convert.ToInt32(admin.user_id),
                    UserName = user.username,
                    Password = user.password,
                }).FirstOrDefault(),
                Conductors = user.Conductors.Select(conductor => new ApiConductor
                {
                    Id = conductor.id,
                    Name = conductor.name,
                    Contact = conductor.contact,
                    UserId = Convert.ToInt32(conductor.user_id),
                    UserName = user.username,
                    Password = user.password,
                }).FirstOrDefault(),
                Parents = user.Parents.Select(parent => new ApiParent
                {
                    Id = parent.id,
                    Name = parent.name,
                    Contact = parent.contact,
                    UserId = Convert.ToInt32(parent.user_id),
                    UserName = user.username,
                    Password = user.password,
                    ChildrenEnroll = Convert.ToInt32(parent.childrenEnroll),
                }).FirstOrDefault(),
                Students = user.Students.Select(student => new ApiStudent
                {
                    PassId = student.passid,
                    Name = student.name,
                    Contact = student.contact,
                    Gender = student.gender,
                    RegNo = student.regno,
                    QrCode = student.qrcode,
                    TotalJourneys = Convert.ToInt32(student.totalJourney),
                    RemainingJourneys = Convert.ToInt32(student.remainingJourney),
                    PassExpiry = student.passExpiray.ToString(),
                    ParentId = Convert.ToInt32(student.parent_id),
                    UserId = Convert.ToInt32(student.user_id),
                    UserName = user.username,
                    Password = user.password,
                }).FirstOrDefault(),
            }).ToList();

            var userMapping = new ClassStructures
            {
                Users = users
            };

            return Request.CreateResponse(HttpStatusCode.OK, userMapping);
        }

        [HttpPost]
        public HttpResponseMessage InsertStudent(ApiStudent student)
        {
            try
            {
                User newUser = new User
                {
                    username = student.UserName,
                    password = student.Password,
                    role = "Student",
                };
                db.Users.Add(newUser);
                db.SaveChanges();

                Student newStudent = new Student
                {
                    name = student.Name,
                    gender = student.Gender,
                    regno = student.RegNo,
                    contact = student.Contact,
                    qrcode = student.QrCode,
                    totalJourney = student.TotalJourneys,
                    remainingJourney = student.RemainingJourneys,
                    passExpiray = DateTime.Parse(student.PassExpiry),
                    parent_id = student.ParentId,
                    user_id = newUser.id,
                };
                db.Students.Add(newStudent);
                db.SaveChanges();
                Parent exitingParent = db.Parents.Find(student.ParentId);
                exitingParent.childrenEnroll += 1;
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Student Inserted Successfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Student");
            }
        }
        public HttpResponseMessage InsertAdmin(ApiAdmin admin)
        {
            try
            {
                User newUser = new User
                {
                    username = admin.UserName,
                    password = admin.Password,
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
                return Request.CreateResponse(HttpStatusCode.OK, "Admin Inserted Successfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Admin");
            }
        }
        public HttpResponseMessage InsertParent(ApiParent parent)
        {
            try
            {
                User newUser = new User
                {
                    username = parent.UserName,
                    password = parent.Password,
                    role = "Parent",
                };
                db.Users.Add(newUser);
                db.SaveChanges();

                Parent newParent = new Parent
                {
                    name = parent.Name,
                    contact = parent.Contact,
                    user_id = newUser.id,
                    childrenEnroll = 0,
                };
                db.Parents.Add(newParent);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "Parent Inserted Successfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Parent");
            }
        }
        public HttpResponseMessage InsertConductor(ApiConductor conductor)
        {
            try
            {
                User newUser = new User
                {
                    username = conductor.UserName,
                    password = conductor.Password,
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
                return Request.CreateResponse(HttpStatusCode.OK, "Conductor Inserted Successfully");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Conductor");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetUserById(int id)
        {
            SingleUser singleUser = new SingleUser();
            User user = db.Users.Find(id);
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
                        ChildrenEnroll = Convert.ToInt32(parentDetails.childrenEnroll),
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
                        Name = studentDetails.name,
                        Contact = studentDetails.contact,
                        PassId = studentDetails.passid,
                        UserId = user.id,
                        Gender = studentDetails.gender,
                        ParentId = Convert.ToInt32(studentDetails.parent_id),
                        PassExpiry = studentDetails.passExpiray.ToString(),
                        Password = user.password,
                        UserName = user.username,
                        QrCode = studentDetails.qrcode,
                        RegNo = studentDetails.regno,
                        RemainingJourneys = Convert.ToInt32(studentDetails.remainingJourney),
                        TotalJourneys = Convert.ToInt32(studentDetails.totalJourney),
                    };
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
        public HttpResponseMessage UpdateCredentials(int id, String username, String oldPassword, String newPassword)
        {
            User user = db.Users.Find(id);
            if (user != null)
            {
                user.username = username;
                if(user.password == oldPassword)
                {
                    user.password = newPassword;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, "User Credentials Updated");
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
        public HttpResponseMessage DeleteUser(int id)
        {
            User user = db.Users.Find(id);
            if (user != null)
            {
                if (user.role == "Student")
                {
                    var student = db.Students.FirstOrDefault(s => s.user_id == id);
                    db.Students.Remove(student);
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
        [HttpGet]
        public HttpResponseMessage Login(string username, string password)
        {
            SingleUser singleUser = new SingleUser();
            User user = db.Users.FirstOrDefault(u =>  u.username == username);
            if (user != null)
            {
                if (user.password == password)
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
                        var parentDetails = db.Parents.FirstOrDefault(p => p.user_id == user.id);
                        ApiParent parent = new ApiParent
                        {
                            Name = parentDetails.name,
                            Contact = parentDetails.contact,
                            ChildrenEnroll = Convert.ToInt32(parentDetails.childrenEnroll),
                            Id = parentDetails.id,
                            UserId = user.id,
                            UserName = user.username,
                            Password = user.password,
                        };
                        singleUser.Parents = parent;
                    }
                    else if (user.role == "Conductor")
                    {
                        var conductorDetails = db.Conductors.FirstOrDefault(c => c.user_id == user.id);
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
                        var studentDetails = db.Students.FirstOrDefault(s => s.user_id == user.id);
                        ApiStudent student = new ApiStudent
                        {
                            Name = studentDetails.name,
                            Contact = studentDetails.contact,
                            PassId = studentDetails.passid,
                            UserId = user.id,
                            Gender = studentDetails.gender,
                            ParentId = Convert.ToInt32(studentDetails.parent_id),
                            PassExpiry = studentDetails.passExpiray.ToString(),
                            Password = user.password,
                            UserName = user.username,
                            QrCode = studentDetails.qrcode,
                            RegNo = studentDetails.regno,
                            RemainingJourneys = Convert.ToInt32(studentDetails.remainingJourney),
                            TotalJourneys = Convert.ToInt32(studentDetails.totalJourney),
                        };
                        singleUser.Students = student;
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
                List<Notification> notifications = db.Notifications.Where(n => n.user_id == id).ToList();
                List<ApiNotification> apiNotifications = new List<ApiNotification>();
                if (notifications.Count > 0)
                {
                    for(int i=0; i<notifications.Count; i++)
                    {
                        apiNotifications.Add(new ApiNotification
                        {
                            Id = notifications[i].id,
                            Date = notifications[i].date.ToString(),
                            Description = notifications[i].description,
                            Time = notifications[i].time.ToString(),
                            Type = notifications[i].type,
                            UserRole = notifications[i].userrole,
                            UserID = Convert.ToInt16(notifications[i].user_id),
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
        [HttpPost]
        public HttpResponseMessage NotifyUser(int  userId, string userRole, string Type, string Description)
        {
            try
            {
                Notification notification = new Notification();
                notification.date = DateTime.Today;
                notification.time = DateTime.Now.TimeOfDay;
                notification.userrole = userRole;
                notification.type = Type;
                notification.description = Description;
                notification.user_id = userId;
                db.Notifications.Add(notification);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, "User Notified");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage MakeAnnouncement(string Description)
        {
            try
            {
                List<User> user = db.Users.ToList();
                for (int i = 0;i < user.Count; i++)
                {
                    Notification notification = new Notification();
                    notification.date = DateTime.Today;
                    notification.time = DateTime.Now.TimeOfDay;
                    notification.userrole = user[i].role;
                    notification.type = "announcement";
                    notification.description = Description;
                    notification.user_id = user[i].id;
                    db.Notifications.Add(notification);
                    db.SaveChanges();
                }
                return Request.CreateResponse(HttpStatusCode.OK, "All Users Notified");
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
    }
}