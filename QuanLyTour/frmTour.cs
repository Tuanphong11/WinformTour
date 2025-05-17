using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;
using OfficeOpenXml;

namespace QuanLyTour
{
    public partial class frmTour : Form
    {
        private string connectionString = KetNoi.str;
        private int selectedTourId = -1;

        public frmTour()
        {
            InitializeComponent();
        }

        private void frmTour_Load(object sender, EventArgs e)
        {
            LoadTourData();
            LoadTourTypes();
            LoadTransports();
        }

        private void LoadTourData()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
SELECT 
    t.TourID, 
    t.TourName, 
    tt.TypeName   AS TourType, 
    tm.MethodName AS Transport,
    t.Price, 
    t.Description, 
    t.StartDate, 
    t.EndDate, 
    t.CreatedAt,
    t.ImageBase64
FROM Tour t
JOIN TourType tt ON t.TourTypeID = tt.TourTypeID
JOIN TransportationMethod tm ON t.TransportID = tm.TransportID
";
                    SqlDataAdapter da = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvTour.DataSource = dt;
                    dgvTour.Columns["ImageBase64"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message);
            }
        }

        private void LoadTourTypes()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TourTypeID, TypeName FROM TourType";
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable table = new DataTable();
                adapter.Fill(table);
                cbTourType.DataSource = table;
                cbTourType.DisplayMember = "TypeName";
                cbTourType.ValueMember = "TourTypeID";
                cbTourType.SelectedIndex = -1;
            }
        }

        private void LoadTransports()
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TransportID, MethodName FROM TransportationMethod";
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable table = new DataTable();
                adapter.Fill(table);
                cbTransport.DataSource = table;
                cbTransport.DisplayMember = "MethodName";
                cbTransport.ValueMember = "TransportID";
                cbTransport.SelectedIndex = -1;
            }
        }

        private void btnChooseImage_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                dlg.Title = "Chọn ảnh tour";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    pictureBox.Image = new Bitmap(dlg.FileName);
                }
            }
        }

        private void dgvTour_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            DataGridViewRow row = dgvTour.Rows[e.RowIndex];
            selectedTourId = Convert.ToInt32(row.Cells["TourID"].Value);
            txtTourName.Text = row.Cells["TourName"].Value.ToString();
            cbTourType.Text = row.Cells["TourType"].Value.ToString();
            cbTransport.Text = row.Cells["Transport"].Value.ToString();
            txtPrice.Text = row.Cells["Price"].Value.ToString().Replace(",00", "");
            txtDescription.Text = row.Cells["Description"].Value.ToString();

            dtpStartDate.Value = Convert.ToDateTime(row.Cells["StartDate"].Value);
            dtpEndDate.Value = Convert.ToDateTime(row.Cells["EndDate"].Value);
            dtpCreatedAt.Value = Convert.ToDateTime(row.Cells["CreatedAt"].Value);

            string imgBase64 = row.Cells["ImageBase64"].Value?.ToString();
            if (!string.IsNullOrEmpty(imgBase64))
            {
                byte[] bytes = Convert.FromBase64String(imgBase64);
                using (var ms = new MemoryStream(bytes))
                    pictureBox.Image = Image.FromStream(ms);
            }
            else
            {
                pictureBox.Image = null;
            }
        }

        private bool ValidateDates(DateTime startDate, DateTime endDate, DateTime createdAt)
        {
            DateTime now = DateTime.Now;
            if (startDate < now)
            {
                MessageBox.Show("Ngày bắt đầu không thể là ngày trong quá khứ!");
                return false;
            }
            if (endDate < now)
            {
                MessageBox.Show("Ngày kết thúc không thể là ngày trong quá khứ!");
                return false;
            }
            if (createdAt <= now)
            {
                MessageBox.Show("Ngày tạo phải lớn hơn thời gian hiện tại!");
                return false;
            }
            if (createdAt > startDate)
            {
                MessageBox.Show("Ngày tạo không thể sau ngày bắt đầu!");
                return false;
            }
            if (startDate > endDate)
            {
                MessageBox.Show("Ngày bắt đầu không thể sau ngày kết thúc!");
                return false;
            }
            return true;
        }

        private bool ValidateDates()
        {
            DateTime now = DateTime.Now;
            if (dtpStartDate.Value < now)
            {
                MessageBox.Show("Ngày bắt đầu không thể là ngày trong quá khứ!");
                return false;
            }
            if (dtpEndDate.Value < now)
            {
                MessageBox.Show("Ngày kết thúc không thể là ngày trong quá khứ!");
                return false;
            }
            if (dtpCreatedAt.Value <= now)
            {
                MessageBox.Show("Ngày tạo phải lớn hơn thời gian hiện tại!");
                return false;
            }
            if (dtpCreatedAt.Value > dtpStartDate.Value)
            {
                MessageBox.Show("Ngày tạo không thể sau ngày bắt đầu!");
                return false;
            }
            if (dtpStartDate.Value > dtpEndDate.Value)
            {
                MessageBox.Show("Ngày bắt đầu không thể sau ngày kết thúc!");
                return false;
            }
            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            if (!ValidateDates())
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
INSERT INTO Tour
  (TourName, TourTypeID, TransportID, Price, Description, StartDate, EndDate, CreatedAt, ImageBase64)
VALUES
  (@TourName, @TourTypeID, @TransportID, @Price, @Description, @StartDate, @EndDate, @CreatedAt, @Image)";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TourName", txtTourName.Text);
                    cmd.Parameters.AddWithValue("@TourTypeID", cbTourType.SelectedValue);
                    cmd.Parameters.AddWithValue("@TransportID", cbTransport.SelectedValue);
                    cmd.Parameters.AddWithValue("@Price", txtPrice.Text);
                    cmd.Parameters.AddWithValue("@Description", txtDescription.Text);
                    cmd.Parameters.AddWithValue("@StartDate", dtpStartDate.Value);
                    cmd.Parameters.AddWithValue("@EndDate", dtpEndDate.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", dtpCreatedAt.Value);

                    string imageBase64 = "";
                    if (pictureBox.Image != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            pictureBox.Image.Save(ms, pictureBox.Image.RawFormat);
                            imageBase64 = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                    cmd.Parameters.AddWithValue("@Image", imageBase64);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Thêm mới tour thành công!");
                LoadTourData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi thêm tour: " + ex.Message);
            }
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            if (selectedTourId == -1)
            {
                MessageBox.Show("Vui lòng chọn tour để sửa!");
                return;
            }
            if (!ValidateDates())
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = @"
UPDATE Tour SET
  TourName    = @TourName,
  TourTypeID  = @TourTypeID,
  TransportID = @TransportID,
  Price       = @Price,
  Description = @Description,
  StartDate   = @StartDate,
  EndDate     = @EndDate,
  CreatedAt   = @CreatedAt,
  ImageBase64 = @Image
WHERE TourID   = @TourID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TourName", txtTourName.Text);
                    cmd.Parameters.AddWithValue("@TourTypeID", cbTourType.SelectedValue);
                    cmd.Parameters.AddWithValue("@TransportID", cbTransport.SelectedValue);
                    cmd.Parameters.AddWithValue("@Price", txtPrice.Text);
                    cmd.Parameters.AddWithValue("@Description", txtDescription.Text);
                    cmd.Parameters.AddWithValue("@StartDate", dtpStartDate.Value);
                    cmd.Parameters.AddWithValue("@EndDate", dtpEndDate.Value);
                    cmd.Parameters.AddWithValue("@CreatedAt", dtpCreatedAt.Value);
                    cmd.Parameters.AddWithValue("@TourID", selectedTourId);

                    string imageBase64 = "";
                    if (pictureBox.Image != null)
                    {
                        using (var ms = new MemoryStream())
                        {
                            pictureBox.Image.Save(ms, pictureBox.Image.RawFormat);
                            imageBase64 = Convert.ToBase64String(ms.ToArray());
                        }
                    }
                    cmd.Parameters.AddWithValue("@Image", imageBase64);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Cập nhật tour thành công!");
                LoadTourData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi sửa tour: " + ex.Message);
            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (selectedTourId == -1)
            {
                MessageBox.Show("Vui lòng chọn tour để xóa!");
                return;
            }
            if (MessageBox.Show("Bạn có chắc muốn xóa tour này?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    string query = "DELETE FROM Tour WHERE TourID = @TourID";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@TourID", selectedTourId);
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
                MessageBox.Show("Xóa tour thành công!");
                LoadTourData();
                ClearInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi xóa tour: " + ex.Message);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            ClearInputs();
        }

        private void ClearInputs()
        {
            txtTourName.Clear();
            cbTourType.SelectedIndex = -1;
            cbTransport.SelectedIndex = -1;
            txtPrice.Clear();
            txtDescription.Clear();
            dtpStartDate.Value = DateTime.Now;
            dtpEndDate.Value = DateTime.Now;
            dtpCreatedAt.Value = DateTime.Now;
            pictureBox.Image = null;
            selectedTourId = -1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            if (dgvTour.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog() { Filter = "Excel Workbook|*.xlsx" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var package = new ExcelPackage())
                    {
                        var ws = package.Workbook.Worksheets.Add("Danh sách Tour");

                        int colIndex = 1;
                        foreach (DataGridViewColumn col in dgvTour.Columns)
                        {
                            if (col.Name != "ImageBase64")
                            {
                                ws.Cells[1, colIndex++].Value = col.HeaderText;
                            }
                        }

                        for (int r = 0; r < dgvTour.Rows.Count; r++)
                        {
                            int currentCol = 1;
                            foreach (DataGridViewColumn col in dgvTour.Columns)
                            {
                                if (col.Name != "ImageBase64")
                                {
                                    var value = dgvTour.Rows[r].Cells[col.Name].Value;
                                    ws.Cells[r + 2, currentCol++].Value = value?.ToString() ?? "";
                                }
                            }
                        }

                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        File.WriteAllBytes(sfd.FileName, package.GetAsByteArray());
                    }

                    MessageBox.Show("Xuất Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    SqlCommand cmd = new SqlCommand();
                    string query = @"
SELECT 
    t.TourID, 
    t.TourName, 
    tt.TypeName AS TourType, 
    tm.MethodName AS Transport,
    t.Price, 
    t.Description, 
    t.StartDate, 
    t.EndDate, 
    t.CreatedAt,
    t.ImageBase64
FROM Tour t
JOIN TourType tt ON t.TourTypeID = tt.TourTypeID
JOIN TransportationMethod tm ON t.TransportID = tm.TransportID
WHERE 1=1 ";

                    if (cbTourType.SelectedIndex != -1)
                    {
                        query += " AND tt.TypeName = @TourType";
                        cmd.Parameters.AddWithValue("@TourType", cbTourType.Text);
                    }

                    if (cbTransport.SelectedIndex != -1)
                    {
                        query += " AND tm.MethodName = @Transport";
                        cmd.Parameters.AddWithValue("@Transport", cbTransport.Text);
                    }

                    if (cbSearchBudget.SelectedIndex != -1)
                    {
                        string selected = cbSearchBudget.Text;
                        if (selected.Contains("Dưới"))
                            query += " AND t.Price < 1000000";
                        else if (selected.Contains("1.000.000 đến 2.000.000"))
                            query += " AND t.Price >= 1000000 AND t.Price <= 2000000";
                        else if (selected.Contains("2.000.000 đến 4.000.000"))
                            query += " AND t.Price > 2000000 AND t.Price <= 4000000";
                        else if (selected.Contains("Trên"))
                            query += " AND t.Price > 4000000";
                    }

                    cmd.CommandText = query;
                    cmd.Connection = conn;

                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    dgvTour.DataSource = dt;
                    dgvTour.Columns["ImageBase64"].Visible = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tìm kiếm: " + ex.Message);
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Đã nhấn nút Nhập dữ liệu!"); // Thêm thông báo để kiểm tra sự kiện
            ImportFromExcel();
        }

        private void ImportFromExcel()
        {
            using (OpenFileDialog ofd = new OpenFileDialog() { Filter = "Excel Workbook|*.xlsx" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using (var package = new ExcelPackage(new FileInfo(ofd.FileName)))
                        {
                            if (package.Workbook.Worksheets.Count == 0)
                            {
                                MessageBox.Show("File Excel không có worksheet nào!");
                                return;
                            }

                            var worksheet = package.Workbook.Worksheets[0];
                            int rowCount = worksheet.Dimension?.Rows ?? 0;

                            if (rowCount <= 1)
                            {
                                MessageBox.Show("File Excel không có dữ liệu để nhập!");
                                return;
                            }

                            bool hasImported = false;
                            for (int row = 2; row <= rowCount; row++)
                            {
                                try
                                {
                                    // Bỏ qua cột TourID, không chèn giá trị từ Excel
                                    string tourName = worksheet.Cells[row, 2].Value?.ToString() ?? "";
                                    string tourType = worksheet.Cells[row, 3].Value?.ToString() ?? "";
                                    string transport = worksheet.Cells[row, 4].Value?.ToString() ?? "";

                                    string priceStr = worksheet.Cells[row, 5].Value?.ToString();
                                    if (string.IsNullOrEmpty(priceStr) || !decimal.TryParse(priceStr, out decimal price))
                                    {
                                        MessageBox.Show($"Lỗi: Giá tại dòng {row} không hợp lệ!");
                                        continue;
                                    }

                                    string description = worksheet.Cells[row, 6].Value?.ToString() ?? "";

                                    string startDateStr = worksheet.Cells[row, 7].Value?.ToString();
                                    string endDateStr = worksheet.Cells[row, 8].Value?.ToString();
                                    string createdAtStr = worksheet.Cells[row, 9].Value?.ToString();

                                    if (!DateTime.TryParse(startDateStr, out DateTime startDate))
                                    {
                                        MessageBox.Show($"Lỗi: Ngày bắt đầu tại dòng {row} không hợp lệ!");
                                        continue;
                                    }
                                    if (!DateTime.TryParse(endDateStr, out DateTime endDate))
                                    {
                                        MessageBox.Show($"Lỗi: Ngày kết thúc tại dòng {row} không hợp lệ!");
                                        continue;
                                    }
                                    if (!DateTime.TryParse(createdAtStr, out DateTime createdAt))
                                    {
                                        MessageBox.Show($"Lỗi: Ngày tạo tại dòng {row} không hợp lệ!");
                                        continue;
                                    }

                                    int tourTypeId = GetTourTypeId(tourType);
                                    int transportId = GetTransportId(transport);

                                    if (tourTypeId == -1)
                                    {
                                        MessageBox.Show($"Lỗi: Không tìm thấy TourType '{tourType}' tại dòng {row}.");
                                        continue;
                                    }
                                    if (transportId == -1)
                                    {
                                        MessageBox.Show($"Lỗi: Không tìm thấy Transport '{transport}' tại dòng {row}.");
                                        continue;
                                    }

                                    if (!ValidateDates(startDate, endDate, createdAt))
                                        continue;

                                    using (SqlConnection conn = new SqlConnection(connectionString))
                                    {
                                        string query = @"
INSERT INTO Tour (TourName, TourTypeID, TransportID, Price, Description, StartDate, EndDate, CreatedAt, ImageBase64)
VALUES (@TourName, @TourTypeID, @TransportID, @Price, @Description, @StartDate, @EndDate, @CreatedAt, @Image)";
                                        SqlCommand cmd = new SqlCommand(query, conn);
                                        cmd.Parameters.AddWithValue("@TourName", tourName);
                                        cmd.Parameters.AddWithValue("@TourTypeID", tourTypeId);
                                        cmd.Parameters.AddWithValue("@TransportID", transportId);
                                        cmd.Parameters.AddWithValue("@Price", price);
                                        cmd.Parameters.AddWithValue("@Description", description);
                                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                                        cmd.Parameters.AddWithValue("@EndDate", endDate);
                                        cmd.Parameters.AddWithValue("@CreatedAt", createdAt);
                                        cmd.Parameters.AddWithValue("@Image", "");

                                        conn.Open();
                                        cmd.ExecuteNonQuery();
                                        hasImported = true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"Lỗi khi nhập dữ liệu tại dòng {row}: {ex.Message}");
                                }
                            }

                            if (hasImported)
                            {
                                MessageBox.Show("Nhập dữ liệu từ Excel thành công!");
                                LoadTourData();
                            }
                            else
                            {
                                MessageBox.Show("Không có dữ liệu nào được nhập do lỗi hoặc không có dòng hợp lệ!");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi khi đọc file Excel: {ex.Message}");
                    }
                }
            }
        }

        private int GetTourTypeId(string tourTypeName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TourTypeID FROM TourType WHERE TypeName = @TypeName";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TypeName", tourTypeName);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
        }

        private int GetTransportId(string transportName)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT TransportID FROM TransportationMethod WHERE MethodName = @MethodName";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@MethodName", transportName);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
        }
    }
}