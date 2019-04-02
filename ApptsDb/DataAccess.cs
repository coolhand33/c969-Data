using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ApptsDb;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace ScheduleIt
{
    public class DataAccess
    {
        

        public static List<customer> GetCustomers()
        {
            using (apptsEntities context = new apptsEntities())
            {
                return context.customers.ToList();
            }
        }

        public static List<appointment> GetUserAppointments(int userId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                var timeNow = DateTime.UtcNow;

                var appts = context.appointments
                    .Include(a => a.customer)
                    .Where(a => a.userId == userId)
                    .ToList();

                //localize the dates
                foreach (var appt in appts)
                {
                    appt.start = DateTime.SpecifyKind(appt.start, DateTimeKind.Utc);
                    appt.end = DateTime.SpecifyKind(appt.end, DateTimeKind.Utc);
                    appt.start = appt.start.ToLocalTime();
                    appt.end = appt.end.ToLocalTime();
                }

                return appts;
            }
        }

        public static List<appointment> GetUserFutureAppointments(int userId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                var timeNow = DateTime.UtcNow;

                var appts = context.appointments
                    .Include(a => a.customer)
                    .Where(a => a.start >= timeNow)
                    .Where(a => a.userId == userId)
                    .ToList();

                //localize the dates
                foreach (var appt in appts)
                {
                    appt.start = DateTime.SpecifyKind(appt.start, DateTimeKind.Utc);
                    appt.end = DateTime.SpecifyKind(appt.end, DateTimeKind.Utc);
                    appt.start = appt.start.ToLocalTime();
                    appt.end = appt.end.ToLocalTime();
                }

                return appts;
            }
        }

        public static List<CustomerDisplay> GetCustomerDisplay()
        {
            using (apptsEntities context = new apptsEntities())
            {
                //I prefer the look and style of linq lambdas. They reduce the amount of code necessary to query the db
                return context.customers
                       .Include(c => c.address.city.country)
                       .Where(c => c.addressId == c.address.addressId)
                       .Where(c => c.address.cityId == c.address.city.cityId)
                       .Where(c => c.address.city.countryId == c.address.city.country.countryId)
                       .Select( c => new CustomerDisplay
                       {
                           Id = c.customerId,
                           Name = c.customerName,
                           Phone = c.address.phone,
                           Address = c.address.address1 + " " + c.address.address2 + ", " + c.address.city.city1 + " " + c.address.postalCode + ", " + c.address.city.country.country1
                       })
                       .OrderBy(c => c.Name)
                       .ToList();
            }
        }

        public static customer GetCustomerById(int custId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                //I prefer the look and style of linq lambdas. They reduce the amount of code necessary to query the db
                return context.customers.Include(c => c.address.city.country)
                    .Where(c => c.customerId == custId)
                    .FirstOrDefault();
            }
        }

        public static appointment GetAppointmentById(int apptId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                //I prefer the look and style of linq lambdas. They reduce the amount of code necessary to query the db
                return context.appointments.Include(a => a.customer)
                    .Where(a => a.appointmentId == apptId)
                    .FirstOrDefault();
            }
        }

        public static int DeleteAppointment(int apptId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                try
                {
                    //I prefer the look and style of linq lambdas. They reduce the amount of code necessary to query the db
                    appointment appt = context.appointments
                        .Where(a => a.appointmentId == apptId)
                        .FirstOrDefault();

                    context.appointments.Remove(appt);

                    return context.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }
            }
        }

        public static void SaveCustomer(customer cust)
        {
            try
            {
                using (apptsEntities context = new apptsEntities())
                {
                    context.Entry(cust).State = cust.customerId == 0 ? EntityState.Added : EntityState.Modified;
                    context.Entry(cust.address).State = cust.address.addressId == 0 ? EntityState.Added : EntityState.Modified;
                    context.Entry(cust.address.city).State = cust.address.city.cityId == 0 ? EntityState.Added : EntityState.Modified;
                    context.Entry(cust.address.city.country).State = cust.address.city.country.countryId == 0 ? EntityState.Added : EntityState.Modified;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static void SaveAppointment(appointment appt)
        {
            try
            {
                using (apptsEntities context = new apptsEntities())
                {
                    //the db is not set up to cascade changes on foreign keys so if the user modifies the chosen customer for an
                    //existing appointment, we have to remove the saved appointment and save a new one with the replacement cust.
                    if(appt.appointmentId != 0)
                    {
                        appointment savedAppt = GetAppointmentById(appt.appointmentId);

                        if(appt.customer.customerId != savedAppt.customerId)
                        {
                            DeleteAppointment(savedAppt.appointmentId);
                            context.Entry(appt).State = EntityState.Added;
                            context.Entry(appt.customer).State = appt.customer.customerId == 0 ? EntityState.Added : EntityState.Modified;
                        }
                        else
                        {
                            context.Entry(appt).State = EntityState.Modified;
                        }
                    }
                    else
                    {
                        context.Entry(appt).State = EntityState.Added;
                        context.Entry(appt.customer).State = appt.customer.customerId == 0 ? EntityState.Added : EntityState.Modified;
                    }

                    context.SaveChanges();
                }
            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static int DeleteCustomer(int custId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                try
                {
                    //I prefer the look and style of linq lambdas. They reduce the amount of code necessary to query the db
                    customer cust = context.customers
                        .Where(c => c.customerId == custId)
                        .FirstOrDefault<customer>();

                    context.customers.Remove(cust);

                    return context.SaveChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return -1;
                }
            }
        }

        public static List<AppointmentDisplay> GetAppointmentDisplay(int userId, string viewType = "Month")
        {
            using (apptsEntities context = new apptsEntities())
            {
                DateTime now = DateTime.Now;
                DateTime fromDate;
                DateTime toDate;
                if(viewType == "Month")
                {
                    fromDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0);
                    //get last day of the month
                    toDate = new DateTime(now.Year, now.AddMonths(1).Month, 1, 0, 0, 0).AddSeconds(-1);
                }
                else
                {
                    //Week view
                    fromDate = DateTime.Today;
                    toDate = fromDate.AddDays(8).AddSeconds(-1);
                }

                //I prefer the look and style of linq lambdas. They reduce the amount of code necessary to query the db
                var appts = context.appointments
                        .Include(a => a.customer)
                        .Where(a => a.userId == userId)
                        .Select(a => new AppointmentDisplay
                        {
                            Id = a.appointmentId,
                            Customer = a.customer.customerName,
                            Contact = a.contact,
                            Title = a.title,
                            Description = a.description,
                            Starts = (DateTime)a.start,
                            Ends = (DateTime)a.end
                        }).ToList();

                //localize the dates
                foreach (var appt in appts)
                {
                    appt.Starts = DateTime.SpecifyKind(appt.Starts, DateTimeKind.Utc);
                    appt.Ends = DateTime.SpecifyKind(appt.Ends, DateTimeKind.Utc);
                    appt.Starts = appt.Starts.ToLocalTime();
                    appt.Ends = appt.Ends.ToLocalTime();
                }

                appts = appts.Where(a => fromDate <= a.Starts && toDate >= a.Ends)
                    .Where(a => a.Ends >= now)
                    .OrderBy(a => a.Starts)
                    .ToList();

                return appts;
            }
        }

        public static DataTable RunReportById(int reportId)
        {
            string query = "";
            switch (reportId)
            {
                case 0:
                    query = "SELECT date_format(start, '%M') as 'Month', type as 'Type', COUNT(*) as 'Count' FROM appointment GROUP BY month, type ORDER BY month";
                    break;
                case 1:
                    query = "SELECT u.userName as 'Consultant', c.customerName as 'Customer', date_format(a.start, '%M') AS 'Month', date_format(a.start, '%a the %D') AS 'Day', date_format(a.start, '%l:%i %p') AS 'Time', concat(timestampdiff(MINUTE, a.start, a.end), ' Minutes') AS 'Length' FROM appointment a, user u, customer c WHERE a.start > now() AND a.customerId = c.customerId ORDER BY u.userName, a.start";
                    break;
                case 2:
                    query = "SELECT date_format(start, '%M, %Y') as 'Month', COUNT(*) as 'Count' FROM appointment GROUP BY month ORDER BY month";
                    break;
            }

            string connectionString = "server=52.206.157.109;user id=U01HfE;password=53687453010;persistsecurityinfo=True;database=U01HfE";
            using(MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();

                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    throw;
                }
                finally
                {
                    conn.Close();
                }
            }

            

        }
    }
}
