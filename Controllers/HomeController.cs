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

        public ActionResult Index()
        {
            // Lấy Top 3 sân bóng hoạt động/sẵn sàng có số lượt đặt sân nhiều nhất
            var sanNoiBat = db.SanBongs
                .Where(s => s.TrangThai == "Hoạt động" || s.TrangThai == "Sẵn sàng")
                .OrderByDescending(s => s.DatSans.Count(d => d.TrangThai != "Đã hủy"))
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