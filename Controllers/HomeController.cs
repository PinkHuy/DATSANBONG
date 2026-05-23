using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using DATSANBONG.Models;

namespace DATSANBONG.Controllers
{
    public class HomeController : Controller
    {
        private readonly DataClasses1DataContext db = new DataClasses1DataContext("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=QuanLySanBong_MVC;Integrated Security=True;MultipleActiveResultSets=True");

        // HomeController.cs
        public ActionResult Index()
        {
            // ✅ Dùng subquery an toàn hơn cho LINQ to SQL
            var sanNoiBat = (from s in db.SanBongs
                             where s.TrangThai == "Hoạt động" || s.TrangThai == "Sẵn sàng"
                             let soLuotDat = db.DatSans
                                 .Count(d => d.MaSan == s.MaSan && d.TrangThai != "Đã hủy")
                             orderby soLuotDat descending
                             select s)
                            .Take(3)
                            .ToList();

            return View(sanNoiBat);
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}