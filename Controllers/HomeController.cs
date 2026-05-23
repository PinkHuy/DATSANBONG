using System.Web.Mvc;
using DATSANBONG.Data;
using DATSANBONG.Models;
using System.Linq;
using System.Collections.Generic;

namespace DATSANBONG.Controllers
{
    public class HomeController : Controller
    {
        private readonly QuanLySanBongDb _db = new QuanLySanBongDb();

        public ActionResult Index()
        {
            // Lấy tất cả sân hoạt động qua ADO.NET (không dùng LINQ to SQL)
            List<SanBong> tatCaSan = _db.LayDanhSachSanBong(chiHoatDong: true);

            // Lấy top 3 sân có nhiều lượt đặt nhất
            var tatCaDatSan = _db.LayTatCaDatSan();

            var sanNoiBat = tatCaSan
                .OrderByDescending(s => tatCaDatSan
                    .Count(d => d.MaSan == s.MaSan && d.TrangThai != "Đã hủy"))
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