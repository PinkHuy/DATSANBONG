using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DATSANBONG.Models;

namespace DATSANBONG.Data
{
    public class QuanLySanBongDb
    {
        private readonly string _connectionString;

        public QuanLySanBongDb()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["QuanLySanBongDb"].ConnectionString;
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        // ─────────────────────────────────────────────────────────────
        //  HASH MẬT KHẨU
        // ─────────────────────────────────────────────────────────────
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                var sb = new StringBuilder();
                foreach (byte b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  NGƯỜI DÙNG
        // ─────────────────────────────────────────────────────────────
        public NguoiDung DangNhap(string taiKhoan, string matKhauHash)
        {
            const string sql = @"SELECT MaND,HoTen,TaiKhoan,MatKhau,SoDienThoai,VaiTro
                                 FROM NguoiDung WHERE TaiKhoan=@TaiKhoan AND MatKhau=@MatKhau";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                cmd.Parameters.AddWithValue("@MatKhau", matKhauHash);
                conn.Open();
                using (var r = cmd.ExecuteReader()) { if (r.Read()) return MapNguoiDung(r); }
            }
            return null;
        }

        public bool TaiKhoanDaTonTai(string taiKhoan)
        {
            const string sql = "SELECT COUNT(1) FROM NguoiDung WHERE TaiKhoan=@TaiKhoan";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public int ThemNguoiDung(NguoiDung nd)
        {
            const string sql = @"INSERT INTO NguoiDung(HoTen,TaiKhoan,MatKhau,SoDienThoai,VaiTro)
                                 VALUES(@HoTen,@TaiKhoan,@MatKhau,@SoDienThoai,@VaiTro);
                                 SELECT SCOPE_IDENTITY();";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@HoTen", nd.HoTen);
                cmd.Parameters.AddWithValue("@TaiKhoan", nd.TaiKhoan);
                cmd.Parameters.AddWithValue("@MatKhau", nd.MatKhau);
                cmd.Parameters.AddWithValue("@SoDienThoai", (object)nd.SoDienThoai ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@VaiTro", nd.VaiTro ?? "Khách hàng");
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<NguoiDung> LayDanhSachNguoiDung()
        {
            const string sql = "SELECT MaND,HoTen,TaiKhoan,MatKhau,SoDienThoai,VaiTro FROM NguoiDung ORDER BY MaND";
            return QueryList(sql, null, MapNguoiDung);
        }

        public NguoiDung LayNguoiDungTheoMa(int maND)
        {
            const string sql = "SELECT MaND,HoTen,TaiKhoan,MatKhau,SoDienThoai,VaiTro FROM NguoiDung WHERE MaND=@MaND";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaND", maND);
                conn.Open();
                using (var r = cmd.ExecuteReader()) { if (r.Read()) return MapNguoiDung(r); }
            }
            return null;
        }

        public void XoaNguoiDung(int maND)
        {
            ExecuteNonQuery("DELETE FROM DatSan WHERE MaND=@MaND", cmd => cmd.Parameters.AddWithValue("@MaND", maND));
            ExecuteNonQuery("DELETE FROM NguoiDung WHERE MaND=@MaND", cmd => cmd.Parameters.AddWithValue("@MaND", maND));
        }

        // ─────────────────────────────────────────────────────────────
        //  LOẠI SÂN
        // ─────────────────────────────────────────────────────────────
        public List<LoaiSan> LayDanhSachLoaiSan()
        {
            const string sql = "SELECT MaLoai,TenLoai,MoTa FROM LoaiSan ORDER BY MaLoai";
            return QueryList(sql, null, r => new LoaiSan
            {
                MaLoai = (int)r["MaLoai"],
                TenLoai = r["TenLoai"].ToString(),
                MoTa = r["MoTa"] == DBNull.Value ? null : r["MoTa"].ToString()
            });
        }

        public LoaiSan LayLoaiSanTheoMa(int maLoai)
        {
            const string sql = "SELECT MaLoai,TenLoai,MoTa FROM LoaiSan WHERE MaLoai=@MaLoai";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaLoai", maLoai);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    if (r.Read()) return new LoaiSan
                    {
                        MaLoai = (int)r["MaLoai"],
                        TenLoai = r["TenLoai"].ToString(),
                        MoTa = r["MoTa"] == DBNull.Value ? null : r["MoTa"].ToString()
                    };
                }
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────
        //  SÂN BÓNG
        // ─────────────────────────────────────────────────────────────
        public List<SanBong> LayDanhSachSanBong(bool chiHoatDong = false)
        {
            string sql = @"SELECT s.MaSan,s.TenSan,s.MaLoai,s.GiaTheoGio,s.HinhAnh,s.TrangThai,s.MaSanCha,
                                  l.TenLoai,l.MoTa AS MoTaLoai
                           FROM SanBong s JOIN LoaiSan l ON s.MaLoai=l.MaLoai";
            if (chiHoatDong) sql += " WHERE s.TrangThai=N'Hoạt động'";
            sql += " ORDER BY s.MaSan";
            return QueryList(sql, null, MapSanBong);
        }

        public SanBong LaySanBongTheoMa(int maSan)
        {
            const string sql = @"SELECT s.MaSan,s.TenSan,s.MaLoai,s.GiaTheoGio,s.HinhAnh,s.TrangThai,s.MaSanCha,
                                        l.TenLoai,l.MoTa AS MoTaLoai
                                 FROM SanBong s JOIN LoaiSan l ON s.MaLoai=l.MaLoai
                                 WHERE s.MaSan=@MaSan";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaSan", maSan);
                conn.Open();
                using (var r = cmd.ExecuteReader()) { if (r.Read()) return MapSanBong(r); }
            }
            return null;
        }

        public int ThemSanBong(SanBong san)
        {
            const string sql = @"INSERT INTO SanBong(TenSan,MaLoai,GiaTheoGio,HinhAnh,TrangThai,MaSanCha)
                                 VALUES(@TenSan,@MaLoai,@GiaTheoGio,@HinhAnh,@TrangThai,@MaSanCha);
                                 SELECT SCOPE_IDENTITY();";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TenSan", san.TenSan);
                cmd.Parameters.AddWithValue("@MaLoai", san.MaLoai);
                cmd.Parameters.AddWithValue("@GiaTheoGio", san.GiaTheoGio);
                cmd.Parameters.AddWithValue("@HinhAnh", (object)san.HinhAnh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", san.TrangThai ?? "Hoạt động");
                cmd.Parameters.AddWithValue("@MaSanCha", (object)san.MaSanCha ?? DBNull.Value);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public void CapNhatSanBong(SanBong san)
        {
            const string sql = @"UPDATE SanBong SET TenSan=@TenSan,MaLoai=@MaLoai,GiaTheoGio=@GiaTheoGio,
                                        HinhAnh=@HinhAnh,TrangThai=@TrangThai,MaSanCha=@MaSanCha
                                 WHERE MaSan=@MaSan";
            ExecuteNonQuery(sql, cmd =>
            {
                cmd.Parameters.AddWithValue("@MaSan", san.MaSan);
                cmd.Parameters.AddWithValue("@TenSan", san.TenSan);
                cmd.Parameters.AddWithValue("@MaLoai", san.MaLoai);
                cmd.Parameters.AddWithValue("@GiaTheoGio", san.GiaTheoGio);
                cmd.Parameters.AddWithValue("@HinhAnh", (object)san.HinhAnh ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", san.TrangThai);
                cmd.Parameters.AddWithValue("@MaSanCha", (object)san.MaSanCha ?? DBNull.Value);
            });
        }

        public void XoaSanBong(int maSan)
        {
            ExecuteNonQuery("DELETE FROM SanBong WHERE MaSan=@MaSan",
                cmd => cmd.Parameters.AddWithValue("@MaSan", maSan));
        }

        // ─────────────────────────────────────────────────────────────
        //  ĐẶT SÂN
        // ─────────────────────────────────────────────────────────────
        public int ThemDatSan(DatSan ds)
        {
            const string sql = @"
                INSERT INTO DatSan(MaND,MaSan,NgayDat,GioBatDau,GioKetThuc,TongTien,GhiChu,TrangThai,maSanCon)
                VALUES(@MaND,@MaSan,@NgayDat,@GioBatDau,@GioKetThuc,@TongTien,@GhiChu,@TrangThai,@MaSanCon);
                SELECT SCOPE_IDENTITY();";

            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaND", ds.MaND);
                cmd.Parameters.AddWithValue("@MaSan", ds.MaSan);
                cmd.Parameters.AddWithValue("@NgayDat", ds.NgayDat);
                cmd.Parameters.AddWithValue("@GioBatDau", ds.GioBatDau);
                cmd.Parameters.AddWithValue("@GioKetThuc", ds.GioKetThuc);
                cmd.Parameters.AddWithValue("@TongTien", ds.TongTien);
                cmd.Parameters.AddWithValue("@GhiChu", (object)ds.GhiChu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@TrangThai", ds.TrangThai ?? "Chờ duyệt");
                // FIX CS1503: MaSanCon là string, cast object đúng rồi
                cmd.Parameters.AddWithValue("@MaSanCon", (object)ds.MaSanCon ?? DBNull.Value);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public List<DatSan> LayLichSuDatSanTheoNguoiDung(int maND)
        {
            const string sql = @"
                SELECT d.MaDatSan,d.MaND,d.MaSan,d.NgayDat,d.GioBatDau,d.GioKetThuc,
                       d.TongTien,d.GhiChu,d.TrangThai,d.maSanCon,
                       s.TenSan,s.GiaTheoGio,s.HinhAnh,s.TrangThai AS TrangThaiSan,s.MaLoai,l.TenLoai
                FROM DatSan d
                JOIN SanBong s  ON d.MaSan=s.MaSan
                JOIN LoaiSan l  ON s.MaLoai=l.MaLoai
                WHERE d.MaND=@MaND
                ORDER BY d.NgayDat DESC,d.GioBatDau DESC";
            return QueryList(sql, cmd => cmd.Parameters.AddWithValue("@MaND", maND), MapDatSan);
        }

        public List<DatSan> LayTatCaDatSan()
        {
            const string sql = @"
                SELECT d.MaDatSan,d.MaND,d.MaSan,d.NgayDat,d.GioBatDau,d.GioKetThuc,
                       d.TongTien,d.GhiChu,d.TrangThai,d.maSanCon,
                       s.TenSan,s.GiaTheoGio,s.HinhAnh,s.TrangThai AS TrangThaiSan,s.MaLoai,l.TenLoai,
                       nd.HoTen,nd.TaiKhoan
                FROM DatSan d
                JOIN SanBong  s  ON d.MaSan=s.MaSan
                JOIN LoaiSan  l  ON s.MaLoai=l.MaLoai
                JOIN NguoiDung nd ON d.MaND=nd.MaND
                ORDER BY d.NgayDat DESC,d.GioBatDau DESC";
            return QueryList(sql, null, r =>
            {
                var ds = MapDatSan(r);
                ds.NguoiDung = new NguoiDung { HoTen = r["HoTen"].ToString(), TaiKhoan = r["TaiKhoan"].ToString() };
                return ds;
            });
        }

        public DatSan LayDatSanTheoMa(int maDatSan)
        {
            const string sql = @"
                SELECT d.MaDatSan,d.MaND,d.MaSan,d.NgayDat,d.GioBatDau,d.GioKetThuc,
                       d.TongTien,d.GhiChu,d.TrangThai,d.maSanCon,
                       s.TenSan,s.GiaTheoGio,s.HinhAnh,s.TrangThai AS TrangThaiSan,s.MaLoai,l.TenLoai
                FROM DatSan d
                JOIN SanBong s ON d.MaSan=s.MaSan
                JOIN LoaiSan l ON s.MaLoai=l.MaLoai
                WHERE d.MaDatSan=@MaDatSan";
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaDatSan", maDatSan);
                conn.Open();
                using (var r = cmd.ExecuteReader()) { if (r.Read()) return MapDatSan(r); }
            }
            return null;
        }

        public bool KiemTraTrungLichPhanCap(int maSan, DateTime ngayDat, TimeSpan gioBatDau, TimeSpan gioKetThuc, int? maDatSanBoQua = null)
        {
            var relatedSanIds = new List<int> { maSan };
            var san = LaySanBongTheoMa(maSan);
            if (san != null)
            {
                if (san.MaSanCha.HasValue) relatedSanIds.Add(san.MaSanCha.Value);
                relatedSanIds.AddRange(LayDanhSachSanBong().Where(s => s.MaSanCha == maSan).Select(s => s.MaSan));
            }

            // FIX CS0029: d.MaSan là int?, dùng .HasValue + .Value
            var bookings = LayTatCaDatSan()
                .Where(d => d.MaSan.HasValue && relatedSanIds.Contains(d.MaSan.Value)
                         && d.NgayDat.Date == ngayDat.Date && d.TrangThai != "Đã hủy")
                .ToList();

            if (maDatSanBoQua.HasValue)
                bookings = bookings.Where(d => d.MaDatSan != maDatSanBoQua.Value).ToList();

            return bookings.Any(b => {
                TimeSpan bEnd = (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59)
                    ? new TimeSpan(24, 0, 0) : b.GioKetThuc;
                return b.GioBatDau < gioKetThuc && bEnd > gioBatDau;
            });
        }

        public bool KiemTraTrungLichSanCon(int maSan, string maSanCon, DateTime ngayDat,
                                            TimeSpan gioBatDau, TimeSpan gioKetThuc,
                                            int? maDatSanBoQua = null)
        {
            // FIX CS0029: d.MaSan là int?, so sánh với int maSan → dùng .HasValue + .Value
            var bookings = LayTatCaDatSan()
                .Where(d => d.MaSan.HasValue && d.MaSan.Value == maSan
                         && string.Equals(d.MaSanCon, maSanCon, StringComparison.OrdinalIgnoreCase)
                         && d.NgayDat.Date == ngayDat.Date
                         && d.TrangThai != "Đã hủy")
                .ToList();

            if (maDatSanBoQua.HasValue)
                bookings = bookings.Where(d => d.MaDatSan != maDatSanBoQua.Value).ToList();

            return bookings.Any(b => {
                TimeSpan bEnd = (b.GioKetThuc.Hours == 23 && b.GioKetThuc.Minutes == 59)
                    ? new TimeSpan(24, 0, 0) : b.GioKetThuc;
                return b.GioBatDau < gioKetThuc && bEnd > gioBatDau;
            });
        }

        public void CapNhatTrangThaiDatSan(int maDatSan, string trangThai)
        {
            ExecuteNonQuery("UPDATE DatSan SET TrangThai=@TrangThai WHERE MaDatSan=@MaDatSan", cmd =>
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThai);
                cmd.Parameters.AddWithValue("@MaDatSan", maDatSan);
            });
        }

        public void CapNhatMaSanConChoDatSan(int maDatSan, string maSanCon)
        {
            ExecuteNonQuery("UPDATE DatSan SET maSanCon=@MaSanCon WHERE MaDatSan=@MaDatSan", cmd =>
            {
                cmd.Parameters.AddWithValue("@MaSanCon", (object)maSanCon ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MaDatSan", maDatSan);
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  THỐNG KÊ / DASHBOARD
        // ─────────────────────────────────────────────────────────────
        public int DemTongDonDat() => (int)ExecuteScalar("SELECT COUNT(1) FROM DatSan");

        public decimal TinhTongDoanhThu() =>
            (decimal)(ExecuteScalar("SELECT ISNULL(SUM(TongTien),0) FROM DatSan WHERE TrangThai=N'Đã duyệt'") ?? 0m);

        public int DemDonDatThang(int thang, int nam) =>
            (int)ExecuteScalar("SELECT COUNT(1) FROM DatSan WHERE MONTH(NgayDat)=@Thang AND YEAR(NgayDat)=@Nam",
                cmd => { cmd.Parameters.AddWithValue("@Thang", thang); cmd.Parameters.AddWithValue("@Nam", nam); });

        public decimal TinhDoanhThuThang(int thang, int nam) =>
            (decimal)(ExecuteScalar("SELECT ISNULL(SUM(TongTien),0) FROM DatSan WHERE TrangThai=N'Đã duyệt' AND MONTH(NgayDat)=@Thang AND YEAR(NgayDat)=@Nam",
                cmd => { cmd.Parameters.AddWithValue("@Thang", thang); cmd.Parameters.AddWithValue("@Nam", nam); }) ?? 0m);

        // ─────────────────────────────────────────────────────────────
        //  CHỦ SÂN
        // ─────────────────────────────────────────────────────────────
        public List<NguoiDung> LayDanhSachNguoiDungTheoVaiTro(string vaiTro)
        {
            const string sql = "SELECT MaND,HoTen,TaiKhoan,MatKhau,SoDienThoai,VaiTro FROM NguoiDung WHERE VaiTro=@VaiTro ORDER BY MaND";
            return QueryList(sql, cmd => cmd.Parameters.AddWithValue("@VaiTro", vaiTro), MapNguoiDung);
        }

        public void CapNhatVaiTro(int maND, string vaiTro)
        {
            ExecuteNonQuery("UPDATE NguoiDung SET VaiTro=@VaiTro WHERE MaND=@MaND", cmd =>
            {
                cmd.Parameters.AddWithValue("@VaiTro", vaiTro);
                cmd.Parameters.AddWithValue("@MaND", maND);
            });
        }

        public void PhanCongSanChoChuSan(int maND, int maSan)
        {
            ExecuteNonQuery("UPDATE SanBong SET MaChuSan=@MaND WHERE MaSan=@MaSan", cmd =>
            {
                cmd.Parameters.AddWithValue("@MaND", maND);
                cmd.Parameters.AddWithValue("@MaSan", maSan);
            });
        }

        public void GoChuSanKhoiTatCaSan(int maND)
        {
            ExecuteNonQuery("UPDATE SanBong SET MaChuSan=NULL WHERE MaChuSan=@MaND",
                cmd => cmd.Parameters.AddWithValue("@MaND", maND));
        }

        public Dictionary<int, List<string>> LaySanCuaTungChuSan()
        {
            const string sql = "SELECT MaChuSan,TenSan FROM SanBong WHERE MaChuSan IS NOT NULL ORDER BY MaChuSan,TenSan";
            var result = new Dictionary<int, List<string>>();
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        int maND = (int)r["MaChuSan"];
                        string ten = r["TenSan"].ToString();
                        if (!result.ContainsKey(maND)) result[maND] = new List<string>();
                        result[maND].Add(ten);
                    }
                }
            }
            return result;
        }

        // ─────────────────────────────────────────────────────────────
        //  SÂN CON
        // ─────────────────────────────────────────────────────────────
        public List<SanCon> LaySanConTheoMaSan(int maSan)
        {
            const string sql = "SELECT MaSanCon,MaSan,TenSanCon,TrangThai FROM SanCon WHERE MaSan=@MaSan AND TrangThai=1";
            var list = new List<SanCon>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaSan", maSan);
                conn.Open();
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                        list.Add(new SanCon
                        {
                            MaSanCon = (int)r["MaSanCon"],
                            MaSan = (int)r["MaSan"],
                            TenSanCon = r["TenSanCon"].ToString(),
                            TrangThai = (bool)r["TrangThai"]
                        });
                }
            }
            return list;
        }

        // ─────────────────────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────
        private List<T> QueryList<T>(string sql, Action<SqlCommand> paramSetter, Func<SqlDataReader, T> mapper)
        {
            var list = new List<T>();
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                paramSetter?.Invoke(cmd);
                conn.Open();
                using (var r = cmd.ExecuteReader()) while (r.Read()) list.Add(mapper(r));
            }
            return list;
        }

        private void ExecuteNonQuery(string sql, Action<SqlCommand> paramSetter)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                paramSetter?.Invoke(cmd);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        private object ExecuteScalar(string sql, Action<SqlCommand> paramSetter = null)
        {
            using (var conn = GetConnection())
            using (var cmd = new SqlCommand(sql, conn))
            {
                paramSetter?.Invoke(cmd);
                conn.Open();
                return cmd.ExecuteScalar();
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  MAPPERS
        // ─────────────────────────────────────────────────────────────
        private static NguoiDung MapNguoiDung(SqlDataReader r) => new NguoiDung
        {
            MaND = (int)r["MaND"],
            HoTen = r["HoTen"].ToString(),
            TaiKhoan = r["TaiKhoan"].ToString(),
            MatKhau = r["MatKhau"].ToString(),
            SoDienThoai = r["SoDienThoai"] == DBNull.Value ? null : r["SoDienThoai"].ToString(),
            VaiTro = r["VaiTro"] == DBNull.Value ? "Khách hàng" : r["VaiTro"].ToString()
        };

        private static SanBong MapSanBong(SqlDataReader r)
        {
            var san = new SanBong
            {
                MaSan = (int)r["MaSan"],
                TenSan = r["TenSan"].ToString(),
                MaLoai = (int?)r["MaLoai"],
                GiaTheoGio = (decimal)r["GiaTheoGio"],
                HinhAnh = r["HinhAnh"] == DBNull.Value ? null : r["HinhAnh"].ToString(),
                TrangThai = r["TrangThai"] == DBNull.Value ? "Hoạt động" : r["TrangThai"].ToString()
            };
            try
            {
                int ord = r.GetOrdinal("MaSanCha");
                san.MaSanCha = r.IsDBNull(ord) ? (int?)null : r.GetInt32(ord);
            }
            catch (IndexOutOfRangeException) { san.MaSanCha = null; }
            return san;
        }

        private static DatSan MapDatSan(SqlDataReader r)
        {
            var ds = new DatSan
            {
                MaDatSan = (int)r["MaDatSan"],
                MaND = (int?)r["MaND"],
                MaSan = (int?)r["MaSan"],
                NgayDat = (DateTime)r["NgayDat"],
                GioBatDau = (TimeSpan)r["GioBatDau"],
                GioKetThuc = (TimeSpan)r["GioKetThuc"],
                TongTien = r["TongTien"] == DBNull.Value ? (decimal?)null : (decimal)r["TongTien"],
                GhiChu = r["GhiChu"] == DBNull.Value ? null : r["GhiChu"].ToString(),
                TrangThai = r["TrangThai"] == DBNull.Value ? "Chờ duyệt" : r["TrangThai"].ToString()
            };
            // FIX CS0029: MaSanCon là string → dùng GetString thay vì GetInt32
            try
            {
                int ord = r.GetOrdinal("maSanCon");
                ds.MaSanCon = r.IsDBNull(ord) ? null : r.GetString(ord);
            }
            catch (IndexOutOfRangeException) { ds.MaSanCon = null; }
            return ds;
        }
    }
}