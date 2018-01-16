/*----------------------------------------------------------------
   // Copyright (C) 2014 山东众阳软件公司
   // 版权所有。 
   //
   // 文件名：DicomUtility.cs
   // 文件功能描述：
   // 
   //
   // 
   // 创建标识： 李志强  2014年2月17日 10:47:40
   // 创建内容： 系统框架和注释
   //
   //
   // 修改标识：
   // 修改原因：
   //
   //
----------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//dicom
using Dicom;
using Dicom.Imaging;
using Dicom.Imaging.Codec;
using Dicom.IO.Buffer;
//
using System.Security.AccessControl;
using System.IO;
using System.Net;
using System.Drawing;
using Dicom.IO.Buffer;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Dicom.Network;

namespace Fleck.MsgServer
{
    /// <summary>
    /// PACS产品DICOM文件处理工具类
    /// </summary>
    public class DicomUtility
    {

        
        /// <summary>
        /// 修正DICOM文件
        /// </summary>
        /// <param name="sourceFile">原始文件路径</param>
        /// <param name="ht">DicomTag与value键与值</param>
        /// <returns>成功则true</returns>
        public static bool ModifyDicom(string sourceFile, System.Collections.Hashtable ht)
        {
            bool bSuc = true;
            try
            {
                //FileSecurity fsec = new FileInfo(sourceFile).GetAccessControl();
                //fsec.SetAccessRule(new FileSystemAccessRule("Everyone", FileSystemRights.FullControl, AccessControlType.Allow));
                DicomFile file = DicomFile.Open(sourceFile);
                Encoding encoding = GetRealEncodingName(file.Dataset.Get<string>(DicomTag.SpecificCharacterSet));
                foreach (System.Collections.DictionaryEntry de in ht)
                {
                    if (de.Value != null)
                    {
                        var item = GetDicomItem((DicomTag)de.Key, encoding, de.Value.ToString());
                        if (item != null)
                        {
                            file.Dataset.Add(item);
                        }
                    }
                    
                }
                string tempFile = System.IO.Path.GetTempFileName();
                file.Save(tempFile);
                System.IO.File.Delete(sourceFile);
                System.IO.File.Move(tempFile, sourceFile);
                System.IO.File.Delete(tempFile);
                
            }
            catch
            {
                throw;
            }
            return bSuc;
        }
        public static  Dicom.DicomItem GetDicomItem(DicomTag tag,Encoding encoding, string value)
        {
            var entry = DicomDictionary.Default[tag];
            if (entry == null)
                throw new DicomDataException("Tag {0} not found in DICOM dictionary. Only dictionary tags may be added implicitly to the dataset.", tag);
            DicomVR vr = null;
            if (value != null)
                vr = entry.ValueRepresentations.FirstOrDefault(x => x.ValueType == typeof(string));
            if (vr == null)
                vr = entry.ValueRepresentations.First();

            if (vr == DicomVR.AS)
            {
                if (value == null)
                    return new DicomAgeString(tag, EmptyBuffer.Value);
                if (typeof(string) == typeof(string))
                    return new DicomAgeString(tag, Dicom.IO.ByteConverter.ToByteBuffer(value ?? String.Empty, encoding, vr.PaddingValue));
            }
           
            if (vr == DicomVR.CS)
            {
                if (value == null)
                    return new DicomCodeString(tag, EmptyBuffer.Value);
                if (typeof(string) == typeof(string))
                    return new DicomCodeString(tag, Dicom.IO.ByteConverter.ToByteBuffer(value ?? String.Empty, encoding, vr.PaddingValue)); 
                if (typeof(string).IsEnum)
                    return new DicomCodeString(tag, Dicom.IO.ByteConverter.ToByteBuffer(value ?? String.Empty, encoding, vr.PaddingValue));
            }

            if (vr == DicomVR.DA)
            {
                if (value == null)
                    return new DicomDate(tag, EmptyBuffer.Value);
                if (typeof(string) == typeof(DateTime))
                    return new DicomDate(tag, value);
                if (typeof(string) == typeof(DicomDateRange))
                    return new DicomDate(tag, value);
                if (typeof(string) == typeof(string))
                    return new DicomDate(tag, value);
            }

            if (vr == DicomVR.DS)
            {
                if (value == null)
                    return new DicomDecimalString(tag, EmptyBuffer.Value);
                if (typeof(string) == typeof(decimal))
                    return new DicomDecimalString(tag, value);
                if (typeof(string) == typeof(string))
                    return new DicomDecimalString(tag, Dicom.IO.ByteConverter.ToByteBuffer(value ?? String.Empty, encoding, vr.PaddingValue));
            }

            if (vr == DicomVR.DT)
            {
                if (value == null)
                    return new DicomDateTime(tag, EmptyBuffer.Value);
                if (typeof(string) == typeof(DateTime))
                    return new DicomDateTime(tag, value.Cast<DateTime>().ToArray());
                if (typeof(string) == typeof(DicomDateRange))
                    return new DicomDateTime(tag, value.Cast<DicomDateRange>().FirstOrDefault() ?? new DicomDateRange());
                if (typeof(string) == typeof(string))
                    return new DicomDateTime(tag, value);
            }
            if (vr == DicomVR.IS)
            {
                if (value == null)
                    return new DicomIntegerString(tag, EmptyBuffer.Value);
                if (typeof(string) == typeof(int))
                    return new DicomIntegerString(tag, value.Cast<int>().ToArray());
                if (typeof(string) == typeof(string))
                    return new DicomIntegerString(tag, value);
            }

            if (vr == DicomVR.LO)
            {
                if (value == null)
                    return new DicomLongString(tag, DicomEncoding.Default, EmptyBuffer.Value);
                if (typeof(string) == typeof(string))
                    return new DicomLongString(tag, encoding, value);
            }
            if (vr == DicomVR.PN)
            {
                if (value == null)
                    return new DicomPersonName(tag, DicomEncoding.Default, EmptyBuffer.Value);
                if (typeof(string) == typeof(string))
                    return new DicomPersonName(tag, encoding, value);
            }

            if (vr == DicomVR.SH)
            {
                if (value == null)
                    return new DicomShortString(tag, DicomEncoding.Default, EmptyBuffer.Value);
                if (typeof(string) == typeof(string))
                    return new DicomShortString(tag, encoding, value);
            }
            return null;

        }
        public static string GetStringFromDicomTag(DicomDataset dataset,DicomTag tag)
        {
            Dicom.IO.Buffer.IByteBuffer  byteBuffer = dataset.Get<IByteBuffer>(tag);
            if (byteBuffer == null || byteBuffer.Size == 0)
            {
                return dataset.Get<string>(tag);
            }
            else
            {
                byte[] buffer = Dicom.IO.ByteConverter.ToArray<byte>(byteBuffer);
                if (dataset.Get<string>(DicomTag.SpecificCharacterSet) == "ISO_IR 192" && 
                    (dataset.Get<string>(DicomTag.Modality) == "US" ||
                    dataset.Get<string>(DicomTag.Modality) == "ES"))
                {
                    return Encoding.GetEncoding("GB2312").GetString(buffer);
                }
                else
                {
                    var encoding = GetRealEncodingName(dataset.Get<string>(DicomTag.SpecificCharacterSet));
                    return encoding.GetString(buffer);
                }
            }
        }
        /// <summary>
        /// 从Dicom SpecificCharacterSet 转换真正的字符编码名称
        /// </summary>
        /// <param name="dicomCharacterSet"></param>
        /// <returns></returns>
        public static Encoding GetRealEncodingName(string dicomCharacterSet)
        {
            switch (dicomCharacterSet)
            {
                case "ISO_IR 100":
                    return System.Text.Encoding.GetEncoding("ISO-8859-1");
                case "ISO_IR 192":
                    return System.Text.Encoding.GetEncoding("UTF-8");
                case "GB18030":
                    return System.Text.Encoding.GetEncoding("GB18030");
                default:
                    return System.Text.Encoding.GetEncoding("ISO-8859-1");
            }
        }
        /// <summary>
        /// 从服务器DCM文件导出图片 李志强
        /// </summary>
        /// <param name="srcDcm">DCM文件</param>
        /// <param name="fileFormat">格式</param>
        /// <returns></returns>
        public  static byte[] GetJpegFromDcm(string srcDcm, System.Drawing.Imaging.ImageFormat fileFormat)
        {
            try
            {
                DicomImage image = new DicomImage(srcDcm);

                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                image.RenderImage().Save(ms, fileFormat);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 从服务器DCM文件导出图片 李志强
        /// </summary>
        /// <param name="srcDcm">DCM文件</param>
        /// <param name="fileFormat">格式</param>
        /// <returns></returns>
        public static byte[] GetJpegFromDcm(string srcDcm, System.Drawing.Imaging.ImageFormat fileFormat,double WindowCenter,double WindowWidth)
        {
            try
            {
                DicomImage image = new DicomImage(srcDcm);
                image.WindowCenter = WindowCenter;
                image.WindowWidth = WindowWidth;
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                image.RenderImage().Save(ms, fileFormat);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 从服务器DCM文件导出图片缩略图
        /// </summary>
        /// <param name="srcDcm"></param>
        /// <param name="fileFormat"></param>
        /// <param name="height"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static byte[] GetJpegThumbnailFromDcm(string srcDcm, System.Drawing.Imaging.ImageFormat fileFormat, int width, int height)
        {
            try
            {
                DicomImage image = new DicomImage(srcDcm);
                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                Bitmap bmp = new Bitmap(width, height);
                //从Bitmap创建一个System.Drawing.Graphics 
                System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(bmp);
                //设置  
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                //下面这个也设成高质量 
                gr.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                //下面这个设成High 
                gr.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                //把原始图像绘制成上面所设置宽高的缩小图 
                System.Drawing.Rectangle rectDestination = new System.Drawing.Rectangle(0, 0, width, height);
                gr.DrawImage(image.RenderImage(), rectDestination, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel);
                bmp.Save(ms, fileFormat);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        /// 从服务器DCM文件导出图片得到相应Base64 李志强
        /// </summary>
        /// <param name="srcDcm">DCM文件</param>
        /// <param name="fileFormat">格式</param>
        /// <returns></returns>
        public static  string GetBase64FromDcm(string srcDcm, System.Drawing.Imaging.ImageFormat fileFormat)
        {
            try
            {
                
                DicomImage image = new DicomImage(srcDcm);

                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                image.RenderImage().Save(ms, fileFormat);
                return System.Convert.ToBase64String(ms.ToArray());
            }
            catch
            {
                return "";
            }
        }
        /// <summary>
        /// 将BMP文件转换成DICOM文件
        /// </summary>
        /// <param name="file"></param>
        public static void Bmp2Dcm(string bmpPath, string dcmPath, int TransferSyntax=-1,string sop_uid="")
        {
            Bitmap bitmap = new Bitmap(bmpPath);
            bitmap = GetValidImage(bitmap);
            int rows, columns;
            byte[] pixels = GetPixelsForDicom(bitmap, out rows, out columns);
            MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);
            DicomDataset dataset = new DicomDataset();
            FillDataset(dataset);
            dataset.Add(DicomTag.PhotometricInterpretation, PhotometricInterpretation.Rgb.Value);
            dataset.Add(DicomTag.Rows, (ushort)rows);
            dataset.Add(DicomTag.Columns, (ushort)columns);
            if (sop_uid != "")
            {
                dataset.Add(DicomTag.SOPInstanceUID, sop_uid);
            }
            DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            pixelData.BitsAllocated = 8;
            pixelData.SamplesPerPixel = 3;
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = 0;
            pixelData.PlanarConfiguration = 0;
            pixelData.AddFrame(buffer);
            DicomFile _dicomfile = new DicomFile(dataset);
            DicomFile file = new DicomFile();
            switch(TransferSyntax)
            {
                case 0:
                   file =  _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.JPEG2000Lossless);
                    break;
                case 1:
                   file =  _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.RLELossless);
                    break;
                case 2:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.JPEGProcess14);
                    break;
                //JPEG Lossy P1 && P4
                case 3:
                    var bits = _dicomfile.Dataset.Get<int>(DicomTag.BitsAllocated, 0, 8);
                    var syntax = DicomTransferSyntax.JPEGProcess1;
                    if (bits == 16)
                    syntax = DicomTransferSyntax.JPEGProcess2_4;
                    file = _dicomfile.ChangeTransferSyntax(syntax, new DicomJpegParams
                    {
                    Quality = 100
                    });
                    break;
                case 4:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian);
                    break;
                case 5:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRBigEndian);
                    break;
                case 6:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ImplicitVRLittleEndian);
                    break;
                case 8:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ImplicitVRBigEndian);
                    break;
                default:
                    file = _dicomfile;
                    break;
            }
           

            file.Save(dcmPath);
        }
        public static void Bmp2Dcm(string bmpPath, string dcmPath, string patient_name, string sex, string age, string process_num, string modality, string hospital_name, string study_description = "", string studydate = "", int TransferSyntax = -1, string sop_uid = "")
        {
            Bitmap bitmap = new Bitmap(bmpPath);
            bitmap = DicomUtility.GetValidImage(bitmap);
            int rows;
            int columns;
            byte[] pixels = DicomUtility.GetPixelsForDicom(bitmap, out rows, out columns);
            MemoryByteBuffer buffer = new MemoryByteBuffer(pixels);
            DicomDataset dataset = new DicomDataset();
            Encoding encoding = Encoding.GetEncoding("GB18030");
            dataset.Add<DicomUID>(DicomTag.SOPClassUID, new DicomUID[]
			{
				DicomUID.SecondaryCaptureImageStorage
			});
            dataset.Add<DicomUID>(DicomTag.StudyInstanceUID, new DicomUID[]
			{
				DicomUtility.GenerateUid()
			});
            dataset.Add<DicomUID>(DicomTag.SeriesInstanceUID, new DicomUID[]
			{
				DicomUtility.GenerateUid()
			});
            dataset.Add<DicomUID>(DicomTag.SOPInstanceUID, new DicomUID[]
			{
				DicomUtility.GenerateUid()
			});
            dataset.Add<string>(DicomTag.PatientID, new string[]
			{
				process_num
			});
            dataset.Add(new DicomItem[]
			{
				DicomUtility.GetDicomItem(DicomTag.PatientName, encoding, patient_name)
			});
            dataset.Add<string>(DicomTag.PatientBirthDate, new string[]
			{
				"00000000"
			});
            dataset.Add(new DicomItem[]
			{
				DicomUtility.GetDicomItem(DicomTag.PatientAge, encoding, age)
			});
            dataset.Add<string>(DicomTag.PatientSex, new string[]
			{
				sex
			});
            if (studydate == "")
            {
                dataset.Add<DateTime>(DicomTag.StudyDate, new DateTime[]
				{
					DateTime.Now
				});
                dataset.Add<DateTime>(DicomTag.StudyTime, new DateTime[]
				{
					DateTime.Now
				});
            }
            else
            {
                dataset.Add<string>(DicomTag.StudyDate, new string[]
				{
					studydate
				});
                dataset.Add<string>(DicomTag.StudyTime, new string[]
				{
					DateTime.Now.ToString("hhmmssfff")
				});
            }
            dataset.Add<string>(DicomTag.AccessionNumber, new string[]
			{
				string.Empty
			});
            dataset.Add<string>(DicomTag.ReferringPhysicianName, new string[]
			{
				string.Empty
			});
            dataset.Add<string>(DicomTag.StudyID, new string[]
			{
				"1"
			});
            dataset.Add<string>(DicomTag.SeriesNumber, new string[]
			{
				"1"
			});
            dataset.Add<string>(DicomTag.ModalitiesInStudy, new string[]
			{
				modality
			});
            dataset.Add<string>(DicomTag.Modality, new string[]
			{
				modality
			});
            dataset.Add<string>(DicomTag.NumberOfStudyRelatedInstances, new string[]
			{
				"1"
			});
            dataset.Add<string>(DicomTag.NumberOfStudyRelatedSeries, new string[]
			{
				"1"
			});
            dataset.Add<string>(DicomTag.NumberOfSeriesRelatedInstances, new string[]
			{
				"1"
			});
            dataset.Add<string>(DicomTag.PatientOrientation, new string[]
			{
				"F/A"
			});
            dataset.Add<string>(DicomTag.ImageLaterality, new string[]
			{
				"U"
			});
            dataset.Add(new DicomItem[]
			{
				DicomUtility.GetDicomItem(DicomTag.InstitutionName, encoding, hospital_name)
			});
            dataset.Add<string>(DicomTag.StudyDescription, new string[]
			{
				study_description
			});
            dataset.Add<string>(DicomTag.PhotometricInterpretation, new string[]
			{
				PhotometricInterpretation.Rgb.Value
			});
            dataset.Add<ushort>(DicomTag.Rows, new ushort[]
			{
				(ushort)rows
			});
            dataset.Add<ushort>(DicomTag.Columns, new ushort[]
			{
				(ushort)columns
			});
            if (sop_uid != "")
            {
                dataset.Add<string>(DicomTag.SOPInstanceUID, new string[]
				{
					sop_uid
				});
            }
            DicomPixelData pixelData = DicomPixelData.Create(dataset, true);
            pixelData.BitsStored = 8;
            pixelData.BitsAllocated = 8;
            pixelData.SamplesPerPixel = 3;
            pixelData.HighBit = 7;
            pixelData.PixelRepresentation = PixelRepresentation.Unsigned;
            pixelData.PlanarConfiguration = PlanarConfiguration.Interleaved;
            pixelData.AddFrame(buffer);
            DicomFile _dicomfile = new DicomFile(dataset);
            DicomFile file = new DicomFile();
            switch (TransferSyntax)
            {
                case 0:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.JPEG2000Lossless, null);
                    goto IL_579;
                case 1:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.RLELossless, null);
                    goto IL_579;
                case 2:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.JPEGProcess14, null);
                    goto IL_579;
                case 3:
                    {
                        int bits = _dicomfile.Dataset.Get<int>(DicomTag.BitsAllocated, 0, 8);
                        DicomTransferSyntax syntax = DicomTransferSyntax.JPEGProcess1;
                        if (bits == 16)
                        {
                            syntax = DicomTransferSyntax.JPEGProcess2_4;
                        }
                        file = _dicomfile.ChangeTransferSyntax(syntax, new DicomJpegParams
                        {
                            Quality = 100
                        });
                        goto IL_579;
                    }
                case 4:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRLittleEndian, null);
                    goto IL_579;
                case 5:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ExplicitVRBigEndian, null);
                    goto IL_579;
                case 6:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ImplicitVRLittleEndian, null);
                    goto IL_579;
                case 8:
                    file = _dicomfile.ChangeTransferSyntax(DicomTransferSyntax.ImplicitVRBigEndian, null);
                    goto IL_579;
            }
            file = _dicomfile;
        IL_579:
            file.Save(dcmPath);
        }
        /// <summary>
        /// 发送单个DCM文件
        /// </summary>
        /// <param name="dcmPath"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        public static void SendDcm(string dcmPath,string ip,int port,string callingAe="ZYPACS",string calledAe="ZYPACS")
        {
            var client = new DicomClient();
            client.AddRequest(new DicomCStoreRequest(dcmPath));
            client.Send(ip, port, false,callingAe, calledAe);
        }
        /// <summary>
        /// 发送DCM文件夹
        /// </summary>
        /// <param name="dcmDir"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        public static void SendDcmDir(string dcmDir, string ip, int port, string callingAe = "ZYPACS", string calledAe = "ZYPACS")
        {
            if (Directory.Exists(dcmDir))
            {
                DirectoryInfo dir = new DirectoryInfo(dcmDir);
                var files = dir.GetFileSystemInfos();
                var client = new DicomClient();
                
                
                foreach (var  file in files)
                {
                    if (file.Attributes != FileAttributes.Directory && file.Attributes !=FileAttributes.Device )
                    {
                        client.AddRequest(new DicomCStoreRequest(file.FullName));
                    }
                }
                client.Send(ip, port, false, callingAe, calledAe);
                
            }
        }
        /// <summary>
        /// 发送多个DCM文件
        /// </summary>
        /// <param name="files"></param>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="callingAe"></param>
        /// <param name="calledAe"></param>
        public static void SendDcmMulti(List<string> files, string ip, int port, string callingAe = "ZYPACS", string calledAe = "ZYPACS")
        {
            var client = new DicomClient();


            foreach (var file in files)
            {
                client.AddRequest(new DicomCStoreRequest(file));
            }
            client.Send(ip, port, false, callingAe, calledAe);
        }
        private static void FillDataset(DicomDataset dataset)
        {

            //type 1 attributes.
            dataset.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            dataset.Add(DicomTag.StudyInstanceUID, GenerateUid());
            dataset.Add(DicomTag.SeriesInstanceUID, GenerateUid());
            dataset.Add(DicomTag.SOPInstanceUID, GenerateUid());

            //type 2 attributes
            dataset.Add(DicomTag.PatientID, "12345");
            dataset.Add(DicomTag.PatientName, string.Empty);
            dataset.Add(DicomTag.PatientBirthDate, "00000000");
            dataset.Add(DicomTag.PatientSex, "M");
            dataset.Add(DicomTag.StudyDate, DateTime.Now);
            dataset.Add(DicomTag.StudyTime, DateTime.Now);
            dataset.Add(DicomTag.AccessionNumber, string.Empty);
            dataset.Add(DicomTag.ReferringPhysicianName, string.Empty);
            dataset.Add(DicomTag.StudyID, "1");
            dataset.Add(DicomTag.SeriesNumber, "1");
            dataset.Add(DicomTag.ModalitiesInStudy, "CR");
            dataset.Add(DicomTag.Modality, "CR");
            dataset.Add(DicomTag.NumberOfStudyRelatedInstances, "1");
            dataset.Add(DicomTag.NumberOfStudyRelatedSeries, "1");
            dataset.Add(DicomTag.NumberOfSeriesRelatedInstances, "1");
            dataset.Add(DicomTag.PatientOrientation, "F/A");
            dataset.Add(DicomTag.ImageLaterality, "U");
        }
        private static DicomUID GenerateUid()
        {
            StringBuilder uid = new StringBuilder();
            uid.Append("1.08.1982.10121984.2.0.07").Append('.').Append(DateTime.UtcNow.Ticks);
            return new DicomUID(uid.ToString(), "SOP Instance UID", DicomUidType.SOPInstance);
        }

        public static Bitmap GetValidImage(Bitmap bitmap)
        {
            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb)
            {
                Bitmap old = bitmap;
                using (old)
                {
                    bitmap = new Bitmap(old.Width, old.Height, PixelFormat.Format24bppRgb);
                    using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        g.DrawImage(old, 0, 0, old.Width, old.Height);
                    }
                }
            }
            return bitmap;
        }
        public  static byte[] GetPixelsForDicom(Bitmap image, out int rows, out int columns)
        {
            rows = image.Height;
            columns = image.Width;

            if (rows % 2 != 0 && columns % 2 != 0)
                --columns;

            BitmapData data = image.LockBits(new Rectangle(0, 0, columns, rows), ImageLockMode.ReadOnly, image.PixelFormat);
            IntPtr bmpData = data.Scan0;
            try
            {
                int stride = columns * 3;
                int size = rows * stride;
                byte[] pixelData = new byte[size];
                for (int i = 0; i < rows; ++i)
                    Marshal.Copy(new IntPtr(bmpData.ToInt64() + i * data.Stride), pixelData, i * stride, stride);

                //swap BGR to RGB
                SwapRedBlue(pixelData);
                return pixelData;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
        public static byte[] GetPixels(Bitmap image, out int rows, out int columns)
        {
            rows = image.Height;
            columns = image.Width;

            if (rows % 2 != 0 && columns % 2 != 0)
                --columns;

            BitmapData data = image.LockBits(new Rectangle(0, 0, columns, rows), ImageLockMode.ReadOnly, image.PixelFormat);
            IntPtr bmpData = data.Scan0;
            try
            {
                int stride = columns * 3;
                int size = rows * stride;
                byte[] pixelData = new byte[size];
                for (int i = 0; i < rows; ++i)
                    Marshal.Copy(new IntPtr(bmpData.ToInt64() + i * data.Stride), pixelData, i * stride, stride);

                //swap BGR to RGB
                //SwapRedBlue(pixelData);
                return pixelData;
            }
            finally
            {
                image.UnlockBits(data);
            }
        }
        public  static void SwapRedBlue(byte[] pixels)
        {
            for (int i = 0; i < pixels.Length; i += 3)
            {
                byte temp = pixels[i];
                pixels[i] = pixels[i + 2];
                pixels[i + 2] = temp;
            }
        }
        public static Image Byte2Image(byte[] pixel,int width,int height,PixelFormat format)
        {
               Bitmap _bitmap;
               GCHandle _handle = GCHandle.Alloc(pixel, GCHandleType.Pinned);
               _bitmap = new Bitmap(width, height, width * 3,format, _handle.AddrOfPinnedObject());
               _handle.Free();
               return _bitmap;
        }
        public static string  GetStrByEncoding(Encoding srcEncoding, Encoding dstEncoding, string srcStr)
        {
            byte[] srcBytes = srcEncoding.GetBytes(srcStr);
      
            byte[] bytes = Encoding.Convert(srcEncoding, dstEncoding, srcBytes);
            string result = dstEncoding.GetString(bytes);
            return result;
        }
        /// <summary>
        /// 获取DICOM文件的字典数据
        /// </summary>
        /// <param name="srcDcm"></param>
        /// <returns></returns>
        public static Dictionary<string, string> GetDcmMeta(string srcDcm)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            DicomImage image = new DicomImage(srcDcm);
            IEnumerator<Dicom.DicomItem> iterator = image.Dataset.GetEnumerator();
            while (iterator.MoveNext())
            {

                if (iterator.Current is Dicom.DicomStringElement)
                {
                    var item = iterator.Current as Dicom.DicomStringElement;
                    if (!dic.ContainsKey(item.Tag.DictionaryEntry.Keyword))
                    {
                        if (item.Tag.DictionaryEntry.Keyword == "WindowWidth")
                        {
                            dic.Add(item.Tag.DictionaryEntry.Keyword, image.WindowWidth.ToString());
                        }
                        else if (item.Tag.DictionaryEntry.Keyword == "WindowCenter")
                        {
                            dic.Add(item.Tag.DictionaryEntry.Keyword, image.WindowCenter.ToString());
                        }
                        else
                        {
                            var item_value = DicomUtility.GetStringFromDicomTag(image.Dataset, item.Tag);
                            if (item_value == null)
                            {
                                if (item.Count > 0)
                                {
                                    item_value = item.Get<string>();
                                }
                            }
                            dic.Add(item.Tag.DictionaryEntry.Keyword, item_value);
                        }

                    }
                }
                if (iterator.Current is Dicom.DicomSignedShort)
                {
                    var item_short = iterator.Current as Dicom.DicomSignedShort;
                    if (!dic.ContainsKey(item_short.Tag.DictionaryEntry.Keyword))
                    {
                        if (item_short.Count > 0)
                        {
                            dic.Add(item_short.Tag.DictionaryEntry.Keyword, item_short.Get<string>());
                        }
                    }
                }
                if (iterator.Current is Dicom.DicomUnsignedShort)
                {
                    var item_short = iterator.Current as Dicom.DicomUnsignedShort;
                    if (!dic.ContainsKey(item_short.Tag.DictionaryEntry.Keyword))
                    {
                        if (item_short.Count > 0)
                        {
                            dic.Add(item_short.Tag.DictionaryEntry.Keyword, item_short.Get<string>());
                        }
                    }
                }
                if (iterator.Current is Dicom.DicomUnsignedLong)
                {
                    var item_short = iterator.Current as Dicom.DicomUnsignedLong;
                    if (!dic.ContainsKey(item_short.Tag.DictionaryEntry.Keyword))
                    {
                        if (item_short.Count > 0)
                        {
                            dic.Add(item_short.Tag.DictionaryEntry.Keyword, item_short.Get<string>());
                        }
                    }
                }

            }




            return dic;
        }

        public static string GetValueByTag(string strDcm,DicomTag tagname)
        {

            DicomImage image = new DicomImage(strDcm);

            string item_value = DicomUtility.GetStringFromDicomTag(image.Dataset, tagname);
            return item_value;
        }
    }
}



