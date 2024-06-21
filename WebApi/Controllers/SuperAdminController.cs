using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using WebApi.Models;

namespace WebApi.Controllers
{
    public class SuperAdminController : ApiController
    {
        BusPassWithQRScanEntities db = new BusPassWithQRScanEntities();
        [HttpGet]
        public HttpResponseMessage GetAllOrganizationDetails()
        {
            try
            {
                var organizations = db.Organizations.ToList();
                if (organizations.Count > 0)
                {
                    SuperAdminDashboardData data = new SuperAdminDashboardData();
                    List<OrganizationsDetails> organizationsDetails = new List<OrganizationsDetails>();
                    data.TotalUsers = db.Users.Count() - 1;
                    for (int i = 0; i < organizations.Count; i++)
                    {
                        int id = Convert.ToInt32(organizations[i].id);
                        organizationsDetails.Add(new OrganizationsDetails
                        {
                            Id = id,
                            Name = organizations[i].name,
                            TotalUsers = db.Users.Where(u => u.organization_id == id).Count(),
                            TotalAdmins = db.Users.Where(u => u.organization_id == id && u.role == "Admin").Count(),
                            TotalConductors = db.Users.Where(u => u.organization_id == id && u.role == "Conductor").Count(),
                            TotalParents = db.Users.Where(u => u.organization_id == id && u.role == "Parent").Count(),
                            TotalStudents = db.Users.Where(u => u.organization_id == id && u.role == "Student").Count(),
                        });
                    }
                    data.Organizations = organizationsDetails;
                    return Request.CreateResponse(HttpStatusCode.OK, data);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Organizations Found!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpGet]
        public HttpResponseMessage GetAllOrganizations()
        {
            try
            {
                var organizations = db.Organizations.ToList();
                List<ApiOrganization> apiOrganizations = new List<ApiOrganization>();
                if (organizations.Count > 0)
                {
                    for (int i = 0; i < organizations.Count; i++)
                    {
                        var apiOrganization = new ApiOrganization
                        {
                            Id = Convert.ToInt32(organizations[i].id),
                            Name = organizations[i].name,
                            Cords = new Location
                            {
                                latitude = Convert.ToDouble(organizations[i].latitude),
                                longitude = Convert.ToDouble(organizations[i].longitude),
                            },
                        };
                        apiOrganizations.Add(apiOrganization);
                    }
                    return Request.CreateResponse(HttpStatusCode.OK, apiOrganizations);
                }
                else
                {
                    return Request.CreateResponse(HttpStatusCode.NotFound, "No Organizations Found!");
                }
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error!");
            }
        }
        [HttpPost]
        public HttpResponseMessage InsertOrganization(ApiOrganization apiOrganization)
        {
            try
            {
                Organization organization = new Organization
                {
                    name = apiOrganization.Name,
                    latitude = apiOrganization.Cords.latitude.ToString(),
                    longitude = apiOrganization.Cords.longitude.ToString(),
                };
                db.Organizations.Add(organization);
                db.SaveChanges();
                return Request.CreateResponse(HttpStatusCode.OK, organization.id);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error Inserting Organization");
            }
        }
    }
}
