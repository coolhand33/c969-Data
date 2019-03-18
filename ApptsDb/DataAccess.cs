using System;
using System.Data.Entity;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ApptsDb
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

        public static List<CustomerDisplay> GetCustomerDisplay()
        {
            using (apptsEntities context = new apptsEntities())
            {
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
                       .ToList();
            }
        }

        public static customer GetCustomerById(int custId)
        {
            using (apptsEntities context = new apptsEntities())
            {
                return context.customers.Include(c => c.address.city.country)
                    .Where(c => c.customerId == custId)
                    .FirstOrDefault();
            }
        }

        public static appointment GetAppointmentById(int apptId)
        {
            using (apptsEntities context = new apptsEntities())
            {
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

                    foreach (var entity in context.ChangeTracker.Entries())
                    {
                        Console.WriteLine("{0}: {1}", entity.Entity.GetType().Name, entity.State);
                    }
                    //context.customers.Add(cust);

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
                    context.appointments.Add(appt);
                    context.Entry(appt).State = appt.appointmentId == 0 ? EntityState.Added : EntityState.Modified;
                    context.Entry(appt.customer).State = appt.customerId == 0 ? EntityState.Added : EntityState.Modified;

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
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

        public static List<AppointmentDisplay> GetAppointmentDisplay(int userId)
        {
            using (apptsEntities context = new apptsEntities())
            {
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

                return appts;
            }
        }
    }
}
