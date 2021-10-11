using System.IO;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XSSF.UserModel;

namespace TestApi.Controllers
{
    [ApiController]
    public class ExcelController : ControllerBase
    {
        [HttpGet("api/export")]
        public FileContentResult Get()
        {
            // data.data
            //创建Excel文件的对象
            var book = new XSSFWorkbook();
            //添加一个sheet
            var sheet = book.CreateSheet("Sheet1");

            //发票头
            sheet.AddMergedRegion(new CellRangeAddress(0, 2, 0, 8));
            var jcRow = sheet.CreateRow(0);
            var jcCell = jcRow.CreateCell(0);
            jcCell.SetCellValue("ZHEJIANG BETTERWAY SUPPLY CHAIN MANAGEMENT CO., LTD");
            var jsStyle = book.CreateCellStyle();
            jsStyle.Alignment = HorizontalAlignment.Center;
            jsStyle.VerticalAlignment = VerticalAlignment.Center;

            var jcFont = book.CreateFont();
            jcFont.FontHeightInPoints = 16;
            jcFont.FontName = "Arial";
            jcFont.IsBold = true;
            jsStyle.SetFont(jcFont);
            jcCell.CellStyle = jsStyle;

            //付款通知
            sheet.AddMergedRegion(new CellRangeAddress(3, 3, 0, 8));
            var pmnRow = sheet.CreateRow(3);
            var pmnCell = pmnRow.CreateCell(0);
            pmnCell.SetCellValue("付款通知");
            var pmnStyle = book.CreateCellStyle();

            pmnStyle.Alignment = HorizontalAlignment.Center;
            pmnStyle.VerticalAlignment = VerticalAlignment.Bottom;
            var pmnFont = book.CreateFont();
            pmnFont.FontHeightInPoints = 16;
            pmnFont.FontName = "宋体";
            pmnFont.IsBold = true;
            pmnStyle.SetFont(pmnFont);
            pmnCell.CellStyle = pmnStyle;


            //notice
            sheet.AddMergedRegion(new CellRangeAddress(4, 4, 0, 8));
            var noticeRow = sheet.CreateRow(4);
            var noticeCell = noticeRow.CreateCell(0);
            noticeCell.SetCellValue("INVOICE");
            var noticeStyle = book.CreateCellStyle();

            noticeStyle.Alignment = HorizontalAlignment.Center;
            noticeStyle.VerticalAlignment = VerticalAlignment.Bottom;
            var noticeFont = book.CreateFont();
            noticeFont.FontHeightInPoints = 16;
            noticeFont.FontName = "Arial";
            noticeFont.IsBold = true;
            noticeStyle.SetFont(noticeFont);
            noticeCell.CellStyle = noticeStyle;

            //(Original)
            sheet.AddMergedRegion(new CellRangeAddress(5, 5, 0, 8));
            var originalRow = sheet.CreateRow(4);
            var originalCell = originalRow.CreateCell(0);
            originalCell.SetCellValue("INVOICE");
            var originalStyle = book.CreateCellStyle();

            originalStyle.Alignment = HorizontalAlignment.Center;
            originalStyle.VerticalAlignment = VerticalAlignment.Bottom;
            var originalFont = book.CreateFont();
            originalFont.FontHeightInPoints = 16;
            originalFont.FontName = "Arial";
            originalFont.IsBold = true;
            originalStyle.SetFont(originalFont);
            originalCell.CellStyle = originalStyle;

            //给sheet1添加第一行的头部标题
            // IRow row1 = sheet.CreateRow(0);
            // row1.CreateCell(0).SetCellValue("始发站");
            // row1.CreateCell(1).SetCellValue("目的地站");
            // row1.CreateCell(2).SetCellValue("目的站站编");
            // row1.CreateCell(3).SetCellValue("口岸");
            // row1.CreateCell(4).SetCellValue("公里数");
            //将数据逐步写入sheet1各个行
            // for (int i = 0; i < info.Count; i++)
            // {
            //     IRow rowtemp = sheet1.CreateRow(i + 1);
            //     rowtemp.CreateCell(0).SetCellValue(info[i].StartStationName);
            //     rowtemp.CreateCell(1).SetCellValue(info[i].EndStationName);
            //     rowtemp.CreateCell(2).SetCellValue(info[i].EndStationCode);
            //     rowtemp.CreateCell(3).SetCellValue(info[i].DomesticPortName);
            //     rowtemp.CreateCell(4).SetCellValue(info[i].Kilometer);
            // }


            using (var ms = new NPOIMemoryStream())
            {
                book.Write(ms);
                // var key = OssPathConstraint.导出全路径("", userId.ToString(), DateTime.Now.ToString("yyyyMMdd") + ".xlsx");
                // var error = _ossService.UploadStreamToOss(ms, key);
                //
                // if (string.IsNullOrWhiteSpace(error))
                //     throw new WebApiInnerException(500, "文件保存失败");

                var fileResult = new FileContentResult(ms.GetBuffer(), "application/octet-stream")
                {
                    FileDownloadName = "test.xlsx"
                };

                return fileResult;
            }
        }
    }

    public class NPOIMemoryStream : MemoryStream
    {
        public bool IsColse { get; private set; }

        public NPOIMemoryStream(bool colse = false) => this.IsColse = colse;

        public override void Close()
        {
            if (!this.IsColse)
                return;
            base.Close();
        }
    }
}