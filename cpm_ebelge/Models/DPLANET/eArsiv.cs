﻿using System;
using System.Runtime.ConstrainedExecution;
using System.Xml.Serialization;

namespace cpm_ebelge.Models.DPLANET
{
    public class eArsiv
    {
        public override string Gonder()
        {
            base.Gonder();
            var dpEArsiv = new DigitalPlanet.ArchiveWebService();
            var dpResult = dpEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, eArsivProtectedValues.createdUBL.IssueDate.Value);

            if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpResult.ServiceResultDescription);

            Connector.m.FaturaUUID = dpResult.Invoices[0].UUID;
            Connector.m.FaturaID = dpResult.Invoices[0].InvoiceId;
            var dpFaturaUBL = dpEArsiv.EArsivIndir(dpResult.Invoices[0].UUID);
            ByteData = dpFaturaUBL.ReturnValue;

            Result.DurumAciklama = dpFaturaUBL.StatusDescription;
            Result.DurumKod = dpFaturaUBL.StatusCode + "";
            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
            Result.EvrakNo = dpFaturaUBL.InvoiceId;
            Result.UUID = dpFaturaUBL.UUID;
            Result.ZarfUUID = "";
            Result.YanitDurum = 0;

            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ByteData);

            if (eArsivProtectedValues.doc.PRINT)
            {
                response.KagitNusha = true;
                response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo + "\nYazdırmak İster Misiniz?";
                response.Dosya = ByteData;
            }
            else
            {
                response.KagitNusha = false;
                response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo;
                response.Dosya = null;
            }

            return response;

        }


        public override string TopluGonder()
        {
            base.TopluGonder();
            var dpEArsiv = new DigitalPlanet.ArchiveWebService();

            foreach (var fatura in Faturalar)
            {
                int EVRAKSN = Convert.ToInt32(fatura.AdditionalDocumentReference.FirstOrDefault(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID")?.ID.Value ?? "0");
                fatura.ID = fatura.ID ?? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") };
                //fatura.ID = fatura.ID ?? new UblInvoiceObject.IDType { Value = "GIB2022000000001 " };
                UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                eArsivProtectedValues.strFatura = serializer.GetXmlAsString(fatura); // XML byte tipinden string tipine dönüştürülür

                dpEArsiv.WebServisAdresDegistir();
                var dpResult = dpEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, fatura.IssueDate.Value);

                if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                {
                    Connector.m.Hata = true;
                    sb.AppendLine(dpResult.ServiceResultDescription);
                    return sb.ToString();
                }
                else
                {
                    foreach (var a in dpResult.Invoices)
                    {
                        foreach (eArsivProtectedValues.doc in Faturalar)
                        {
                            if (eArsivProtectedValues.doc.UUID.Value == a.UUID)
                            {
                                sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + a.InvoiceId);

                                Result.DurumAciklama = a.StatusDescription;
                                Result.DurumKod = a.StatusCode + "";
                                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                                Result.EvrakNo = a.InvoiceId;
                                Result.UUID = a.UUID;
                                Result.ZarfUUID = "";
                                Result.YanitDurum = 0;

                                Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(eArsivProtectedValues.doc.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), ser.GetXmlAsByteArray(eArsivProtectedValues.doc));
                                break;
                            }
                        }
                    }
                }
            }
            return sb.ToString();

        }

        public override string Iptal()
        {
            base.Iptal();
            var dpEArsiv = new DigitalPlanet.ArchiveWebService();
            var dpResult = dpEArsiv.IptalFatura(GUID, TUTAR);
            if (dpResult.ServiceResult == COMMON.dpInvoice.Result.Error)
                throw new Exception(dpResult.ServiceResultDescription);
            else
                Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, dpResult.StatusDescription, DateTime.Now);

            return "eArşiv Fatura Başarıyla İptal Edilmiştir ve İlgili eArşiv Fatura Silinmiştir!";

        }


        public override string Itiraz()
        {
            base.Itiraz();
            var dpArsiv = new DigitalPlanet.ArchiveWebService();
            dpArsiv.WebServisAdresDegistir();
            dpArsiv.EArsivItiraz();
            return "Test!";

        }

        public override string GonderilenGuncelle()
        {
            base.GonderilenGuncelle();
            var Result = new Results.EFAGDN();
            var dpArsiv = new DigitalPlanet.ArchiveWebService();
            dpArsiv.WebServisAdresDegistir();

            var uuid = Entegrasyon.GetUUIDFromEvraksn(new[] { EVRAKSN }.ToList());

            if (uuid[0] != "")
            {
                var dpArs = dpArsiv.EArsivIndir(uuid[0]);
                XmlSerializer ser = new XmlSerializer(typeof(InvoiceType));

                var byteData = dpArsiv.EArsivIndir(dpArs.UUID).ReturnValue;
                //File.WriteAllBytes("test.xml", byteData);
                var i = (InvoiceType)ser.Deserialize(new MemoryStream(byteData));
                if (i.AdditionalDocumentReference.First(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID").ID.Value == EVRAKSN + "")
                {
                    Result.DurumAciklama = dpArs.StatusDescription;
                    Result.DurumKod = dpArs.StatusCode + "";
                    Result.DurumZaman = DateTime.Now;
                    Result.EvrakNo = dpArs.InvoiceId;
                    Result.UUID = dpArs.UUID;
                    Result.ZarfUUID = "";
                    Result.YanitDurum = 0;

                    Entegrasyon.UpdateEfagdn(Result, EVRAKSN, byteData, true);
                }
            }
            else
            {
                var dt = Entegrasyon.GetGonderimZaman(new[] { EVRAKSN }.ToList())[0];

                if (dt == new DateTime(1900, 1, 1))
                    dt = DateTime.Now;

                var start = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
                var end = new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);

                var invoices = dpArsiv.EArsivIndir(start, end);

                XmlSerializer ser = new XmlSerializer(typeof(InvoiceType));

                foreach (var inv in invoices.Invoices)
                {
                    var byteData = dpArsiv.EArsivIndir(inv.UUID).ReturnValue;
                    //File.WriteAllBytes("test.xml", byteData);
                    var i = (InvoiceType)ser.Deserialize(new MemoryStream(byteData));
                    if (i.AdditionalDocumentReference.First(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID").ID.Value == EVRAKSN + "")
                    {
                        Result.DurumAciklama = inv.StatusDescription;
                        Result.DurumKod = inv.StatusCode + "";
                        Result.DurumZaman = DateTime.Now;
                        Result.EvrakNo = inv.InvoiceId;
                        Result.UUID = inv.UUID;
                        Result.ZarfUUID = "";
                        Result.YanitDurum = 0;

                        Entegrasyon.UpdateEfagdn(Result, EVRAKSN, byteData, true);
                    }
                }
            }
            break;

        }


        public override string GonderilenGuncelle()
        {
            base.GonderilenGuncelle();
            var dpArsiv = new DigitalPlanet.ArchiveWebService();
            dpArsiv.WebServisAdresDegistir();

            var uuid = Entegrasyon.GetUUIDFromEvraksn(new[] { EVRAKSN }.ToList());

            if (uuid[0] != "")
            {
                var dpArs = dpArsiv.EArsivIndir(uuid[0]);
                XmlSerializer ser = new XmlSerializer(typeof(InvoiceType));

                var byteData = dpArsiv.EArsivIndir(dpArs.UUID).ReturnValue;
                //File.WriteAllBytes("test.xml", byteData);
                var i = (InvoiceType)ser.Deserialize(new MemoryStream(byteData));
                if (i.AdditionalDocumentReference.First(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID").ID.Value == EVRAKSN + "")
                {
                    Result.DurumAciklama = dpArs.StatusDescription;
                    Result.DurumKod = dpArs.StatusCode + "";
                    Result.DurumZaman = DateTime.Now;
                    Result.EvrakNo = dpArs.InvoiceId;
                    Result.UUID = dpArs.UUID;
                    Result.ZarfUUID = "";
                    Result.YanitDurum = 0;

                    Entegrasyon.UpdateEfagdn(Result, EVRAKSN, byteData, true);
                }
            }
            else
            {
                var dt = Entegrasyon.GetGonderimZaman(new[] { EVRAKSN }.ToList())[0];

                if (dt == new DateTime(1900, 1, 1))
                    dt = DateTime.Now;

                var start = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0);
                var end = new DateTime(dt.Year, dt.Month, dt.Day, 23, 59, 59);

                var invoices = dpArsiv.EArsivIndir(start, end);

                XmlSerializer ser = new XmlSerializer(typeof(InvoiceType));

                foreach (var inv in invoices.Invoices)
                {
                    var byteData = dpArsiv.EArsivIndir(inv.UUID).ReturnValue;
                    //File.WriteAllBytes("test.xml", byteData);
                    var i = (InvoiceType)ser.Deserialize(new MemoryStream(byteData));
                    if (i.AdditionalDocumentReference.First(elm => elm.DocumentTypeCode.Value == "CUST_INV_ID").ID.Value == EVRAKSN + "")
                    {
                        Result.DurumAciklama = inv.StatusDescription;
                        Result.DurumKod = inv.StatusCode + "";
                        Result.DurumZaman = DateTime.Now;
                        Result.EvrakNo = inv.InvoiceId;
                        Result.UUID = inv.UUID;
                        Result.ZarfUUID = "";
                        Result.YanitDurum = 0;

                        Entegrasyon.UpdateEfagdn(Result, EVRAKSN, byteData, true);
                    }
                }
            }
            break;

        }





    }
}
