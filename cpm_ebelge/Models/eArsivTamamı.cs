namespace cpm_ebelge.Models
{
     public class eArsivProtectedValues
        {
            public dynamic doc { get; set; }
            public BaseUbl createdUBL { get; set; }
            public dynamic schematronResult {get; set; }
            public dynamic strFatura { get; set; }
            }

            public abstract class EArsiv : EArsiv_EMüstahsil
        {
            protected eArsivProtectedValues eArsivProtectedValues { get; set; } = new eArsivProtectedValues();

            public static EArsivYanit Gonder(int EVRAKSN)
            {
                EArsivYanit response = new EArsivYanit();
                var Result = new Results.EFAGDN();
                eArsivProtectedValues.doc = GeneralCreator.GetUBLArchiveData(EVRAKSN);
                if (eArsivProtectedValues.doc != null)
                {
                    Connector.m.PkEtiketi = doc.PK;
                        //doc.BaseUBL.ID.Value = doc.SendId ? doc.BaseUBL.ID.Value : "";
                        eArsivProtectedValues.createdUBL = doc.BaseUBL;  // e-Arşiv fatura UBL i oluşturulur

                    if (UrlModel.SelectedItem == "QEF")
                    {
                        if ( eArsivProtectedValues.createdUBL.Note == null)
                            eArsivProtectedValues.createdUBL.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                        var noteList =  eArsivProtectedValues.createdUBL.Note.ToList();
                        noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                         eArsivProtectedValues.createdUBL.Note = noteList.ToArray();
                    }

                    UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                    if (UrlModel.SelectedItem == "DPLANET")
                         eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") } :  eArsivProtectedValues.createdUBL.ID;

                    // eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "GIB2022000000001" } :  eArsivProtectedValues.createdUBL.ID;

                    if (Connector.m.SchematronKontrol)
                    {
                        eArsivProtectedValues.schematronResult = SchematronChecker.Check( eArsivProtectedValues.createdUBL, SchematronDocType.eArsiv);
                        if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                    }
                    eArsivProtectedValues.strFatura = serializer.GetXmlAsString( eArsivProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür

                    Entegrasyon.EfagdnUuid(EVRAKSN, eArsivProtectedValues.doc.BaseUBL.UUID.Value);
                }
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
                public override string Gonder()
                {
                    base.Gonder();
                    var dpEArsiv = new DigitalPlanet.ArchiveWebService();
                    var dpResult = dpEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.IssueDate.Value);

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
            }
            public sealed class EDMEArsiv : EArsiv
            {
                public override string Gonder()
                {
                    base.Gonder();
                    var edmEArsiv = new EDM.ArchiveWebService();
                    var edmResult = edmEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.AccountingCustomerParty.Party.PartyIdentification[0].ID.Value, Connector.m.VknTckn,  eArsivProtectedValues.createdUBL?.AccountingCustomerParty?.Party?.Contact?.ElectronicMail?.Value ?? "");

                    Connector.m.FaturaUUID = edmResult.INVOICE[0].UUID;
                    Connector.m.FaturaID = edmResult.INVOICE[0].ID;
                    var edmFaturaUBL = edmEArsiv.EArsivIndir(edmResult.INVOICE[0].UUID);
                    ByteData = edmFaturaUBL[0].CONTENT.Value;

                    Result.DurumAciklama = edmFaturaUBL[0].HEADER.STATUS_DESCRIPTION;
                    Result.DurumKod = "";
                    Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                    Result.EvrakNo = edmResult.INVOICE[0].ID;
                    Result.UUID = edmResult.INVOICE[0].UUID;
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
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                public override string Gonder()
                {
                    base.Gonder();
                    var qefEArsiv = new QEF.ArchiveService();
                    eArsivProtectedValues.doc.SUBE = eArsivProtectedValues.doc.SUBE == "default" ? "DFLT" : eArsivProtectedValues.doc.SUBE;
                    var qefResult = qefEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, Connector.m.VknTckn, EVRAKSN, eArsivProtectedValues.doc.SUBE, eArsivProtectedValues.doc.BaseUBL.IssueDate.Value.Date);

                    if (qefResult.Result.resultText != "İşlem başarılı.")
                        throw new Exception(qefResult.Result.resultText);

                    ByteData = qefResult.Belge.belgeIcerigi;

                    Result.DurumAciklama = qefResult.Result.resultText;
                    Result.DurumKod = qefResult.Result.resultCode;
                    Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                    Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                    Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
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
            }

            public static string TopluGonder(List<InvoiceType> Faturalar, List<string> Subeler, bool FarkliSube)
            {
                StringBuilder sb = new StringBuilder();
                var Result = new Results.EFAGDN();

                UBLBaseSerializer ser = new InvoiceSerializer();
                if (Connector.m.SchematronKontrol)
                {
                    foreach (var Fatura in Faturalar)
                    {
                        eArsivProtectedValues.schematronResult = SchematronChecker.Check(Fatura, SchematronDocType.eArsiv);
                        if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                    }
                }
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                public override string TopluGonder()
                {

                    base.TopluGonder();
                    var fitEArsiv = new FIT.ArchiveWebService();
                    fitEArsiv.WebServisAdresDegistir();
                    //Connector.m.PkEtiketi = PKETIKET;
                    if (!FarkliSube)
                    {
                        var fitResult = fitEArsiv.TopluEArsivGonder(Faturalar, Subeler[0]);

                        foreach (var a in fitResult.preCheckSuccessResults)
                        {
                            foreach (var eArsivProtectedValues.doc in Faturalar)
                            {
                                if (eArsivProtectedValues.doc.UUID.Value == a.UUID)
                                {
                                    eArsivProtectedValues.doc.ID.Value = a.InvoiceNumber;
                                    sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + a.InvoiceNumber);
                                    eArsivProtectedValues.doc.ID.Value = a.InvoiceNumber;

                                    Result.DurumAciklama = a.SuccessDesc;
                                    Result.DurumKod = a.SuccessCode + "";
                                    Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                                    Result.EvrakNo = a.InvoiceNumber;
                                    Result.UUID = a.UUID;
                                    Result.ZarfUUID = "";
                                    Result.YanitDurum = 0;

                                    Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(eArsivProtectedValues.doc.AdditionalDocumentReference.Where(element => element.DocumentTypeCode?.Value == "CUST_INV_ID").First().ID.Value), ser.GetXmlAsByteArray(eArsivProtectedValues.doc));
                                    break;
                                }
                            }
                        }
                        foreach (var b in fitResult.preCheckErrorResults)
                        {
                            sb.AppendLine(String.Format("Hatalı e-Arşiv Fatura:{0} - Hata:{1}({2})", b.InvoiceNumber, b.ErrorDesc, b.ErrorCode));
                        }
                        if (fitResult.Result.Result1 == ResultType.FAIL)
                            sb.AppendLine(fitResult.Detail);
                    }
                    else
                    {
                        int i = 0;
                        foreach (var fatura in Faturalar)
                        {
                            UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                            eArsivProtectedValues.strFatura = serializer.GetXmlAsString(fatura); // XML byte tipinden string tipine dönüştürülür

                            var fitResult = fitEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, fatura.UUID.Value, Subeler[i]);
                            i++;

                            Connector.m.FaturaUUID = fitResult.preCheckSuccessResults[0].UUID;
                            Connector.m.FaturaID = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                            //var fitFaturaUBL = fitEArsiv.FaturaUBLIndir();
                            //ByteData = fitFaturaUBL.binaryData;

                            Result.DurumAciklama = fitResult.preCheckSuccessResults[0].SuccessDesc;
                            Result.DurumKod = fitResult.preCheckSuccessResults[0].SuccessCode + "";
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                            Result.UUID = fitResult.preCheckSuccessResults[0].UUID;
                            Result.ZarfUUID = "";
                            Result.YanitDurum = 0;
                            sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);

                            Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(fatura.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), null);
                            if (Connector.m.DokumanIndir)
                            {
                                var Gonderilen = fitEArsiv.ImzaliIndir(Result.UUID, "", 0);
                                Entegrasyon.UpdateEfados(Gonderilen.binaryData);
                            }
                        }
                    }
                    return sb.ToString();
                }
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
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
            }
            public sealed class EDMEArsiv : EArsiv
            {
                public override string TopluGonder()
                {
                    base.TopluGonder();
                    var edmEArsiv = new EDM.ArchiveWebService();
                    edmEArsiv.WebServisAdresDegistir();
                    var edmResult = edmEArsiv.TopluEArsivGonder(Faturalar);

                    foreach (var a in edmResult.INVOICE)
                    {
                        foreach (var eArsivProtectedValues.doc in Faturalar)
                        {
                            if (eArsivProtectedValues.doc.UUID.Value == a.UUID)
                            {
                                sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + a.ID);

                                Result.DurumAciklama = a.HEADER.STATUS_DESCRIPTION;
                                Result.DurumKod = "";
                                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                                Result.EvrakNo = a.ID;
                                Result.UUID = a.UUID;
                                Result.ZarfUUID = a.HEADER.ENVELOPE_IDENTIFIER ?? "";
                                Result.YanitDurum = 0;

                                Entegrasyon.UpdateEfagdn(Result, Convert.ToInt32(eArsivProtectedValues.doc.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value), ser.GetXmlAsByteArray(eArsivProtectedValues.doc));
                                break;
                            }
                        }
                    }
                    return sb.ToString();
                }
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                public override string TopluGonder()
                {
                    base.TopluGonder();
                    var qefEArsiv = new QEF.ArchiveService();

                    foreach (var fatura in Faturalar)
                    {
                        if (fatura.Note == null)
                            fatura.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                        var noteList = fatura.Note.ToList();
                        noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                        fatura.Note = noteList.ToArray();

                        UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                        eArsivProtectedValues.strFatura = serializer.GetXmlAsString(fatura); // XML byte tipinden string tipine dönüştürülür

                        var sube = "DFLT";

                        if (Subeler.Count > 0)
                            sube = Subeler[0];

                        sube = sube == "default" ? "DFLT" : sube;
                        var EVRAKSN = Convert.ToInt32(fatura.AdditionalDocumentReference.Where(element => element.DocumentTypeCode.Value == "CUST_INV_ID").First().ID.Value);
                        var qefResult = qefEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, Connector.m.VknTckn, EVRAKSN, sube, fatura.IssueDate.Value.Date);

                        if (qefResult.Result.resultText != "İşlem başarılı.")
                            throw new Exception(qefResult.Result.resultText);

                        var ByteData = qefResult.Belge.belgeIcerigi;

                        Result.DurumAciklama = qefResult.Result.resultText;
                        Result.DurumKod = qefResult.Result.resultCode;
                        Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                        Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                        Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
                        Result.ZarfUUID = "";
                        Result.YanitDurum = 0;

                        Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ByteData);

                        sb.AppendLine("e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + Result.EvrakNo);
                    }
                    return sb.ToString();
                }
            }
            // Gonderde EArsivYanıt var burada beyaz geliyor  
            public override string EArsivYanit Gonder(int EVRAKSN)
            {
                EArsivYanit response = new EArsivYanit();
                var Result = new Results.EFAGDN();
                eArsivProtectedValues.doc = GeneralCreator.GetUBLArchiveData(EVRAKSN);
                if (eArsivProtectedValues.doc != null)
                {
                    Connector.m.PkEtiketi = doc.PK;
                    //doc.BaseUBL.ID.Value = doc.SendId ? doc.BaseUBL.ID.Value : "";
                    var  eArsivProtectedValues.createdUBL = doc.BaseUBL;  // e-Arşiv fatura UBL i oluşturulur

                    if (UrlModel.SelectedItem == "QEF")
                    {
                        if ( eArsivProtectedValues.createdUBL.Note == null)
                             eArsivProtectedValues.createdUBL.Note = new List<UblInvoiceObject.NoteType>().ToArray();

                        var noteList =  eArsivProtectedValues.createdUBL.Note.ToList();
                        noteList.Add(new UblInvoiceObject.NoteType { Value = "Gönderim Şekli: ELEKTRONIK" });

                         eArsivProtectedValues.createdUBL.Note = noteList.ToArray();
                    }

                    UBLBaseSerializer serializer = new InvoiceSerializer();  // UBL  XML e dönüştürülür
                    if (UrlModel.SelectedItem == "DPLANET")
                         eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "CPM" + DateTime.Now.Year + EVRAKSN.ToString("000000000") } :  eArsivProtectedValues.createdUBL.ID;

                    // eArsivProtectedValues.createdUBL.ID =  eArsivProtectedValues.createdUBL.ID.Value == $"CPM{DateTime.Now.Year}000000001" ? new UblInvoiceObject.IDType { Value = "GIB2022000000001" } :  eArsivProtectedValues.createdUBL.ID;

                    if (Connector.m.SchematronKontrol)
                    {
                        eArsivProtectedValues.schematronResult = SchematronChecker.Check( eArsivProtectedValues.createdUBL, SchematronDocType.eArsiv);
                        if (eArsivProtectedValues.schematronResult.SchemaResult != "Başarılı" || eArsivProtectedValues.schematronResult.SchematronResult != "Başarılı")
                            throw new Exception(eArsivProtectedValues.schematronResult.Detail);
                    }
                    eArsivProtectedValues.strFatura = serializer.GetXmlAsString( eArsivProtectedValues.createdUBL); // XML byte tipinden string tipine dönüştürülür

                    Entegrasyon.EfagdnUuid(EVRAKSN, eArsivProtectedValues.doc.BaseUBL.UUID.Value);
                    switch (UrlModel.SelectedItem)
                    {
                        case "FIT":
                        case "ING":
                        case "INGBANK":
                            var fitEArsiv = new FIT.ArchiveWebService();
                            var fitResult = fitEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.UUID.Value, eArsivProtectedValues.doc.SUBE);
                            byte[] ByteData = null;
                            if (fitResult.Result.Result1 == ResultType.SUCCESS)
                            {
                                Connector.m.FaturaUUID = fitResult.preCheckSuccessResults[0].UUID;
                                Connector.m.FaturaID = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                                //var fitFaturaUBL = fitEArsiv.FaturaUBLIndir();
                                //ByteData = fitFaturaUBL.binaryData;

                                Result.DurumAciklama = fitResult.preCheckSuccessResults[0].SuccessDesc;
                                Result.DurumKod = fitResult.preCheckSuccessResults[0].SuccessCode + "";
                                Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                                Result.EvrakNo = fitResult.preCheckSuccessResults[0].InvoiceNumber;
                                Result.UUID = fitResult.preCheckSuccessResults[0].UUID;
                                Result.ZarfUUID = "";
                                Result.YanitDurum = 0;

                                Entegrasyon.UpdateEfagdn(Result, EVRAKSN, null);
                                if (Connector.m.DokumanIndir)
                                {
                                    var Gonderilen = fitEArsiv.ImzaliIndir(Result.UUID, "", 0);
                                    Entegrasyon.UpdateEfados(Gonderilen.binaryData);
                                }
                            }
                            if (fitResult.Result.Result1 == ResultType.SUCCESS)
                            {
                                if (eArsivProtectedValues.doc.PRINT)
                                {
                                    response.KagitNusha = true;
                                    response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + fitResult.preCheckSuccessResults[0].InvoiceNumber + "\nYazdırmak İster Misiniz?";
                                    response.Dosya = ByteData;
                                }
                                else
                                {
                                    response.KagitNusha = false;
                                    response.Mesaj = "e-Arşiv Fatura başarıyla gönderildi. \nEvrak No: " + fitResult.preCheckSuccessResults[0].InvoiceNumber;
                                    response.Dosya = null;
                                }
                            }
                            else
                            {
                                throw new Exception(fitResult.preCheckErrorResults[0].ErrorDesc);
                            }
                            return response;
                        case "DPLANET":
                            var dpEArsiv = new DigitalPlanet.ArchiveWebService();
                            var dpResult = dpEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.IssueDate.Value);

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
                        case "EDM":
                            var edmEArsiv = new EDM.ArchiveWebService();
                            var edmResult = edmEArsiv.EArsivGonder(eArsivProtectedValues.strFatura,  eArsivProtectedValues.createdUBL.AccountingCustomerParty.Party.PartyIdentification[0].ID.Value, Connector.m.VknTckn,  eArsivProtectedValues.createdUBL?.AccountingCustomerParty?.Party?.Contact?.ElectronicMail?.Value ?? "");

                            Connector.m.FaturaUUID = edmResult.INVOICE[0].UUID;
                            Connector.m.FaturaID = edmResult.INVOICE[0].ID;
                            var edmFaturaUBL = edmEArsiv.EArsivIndir(edmResult.INVOICE[0].UUID);
                            ByteData = edmFaturaUBL[0].CONTENT.Value;

                            Result.DurumAciklama = edmFaturaUBL[0].HEADER.STATUS_DESCRIPTION;
                            Result.DurumKod = "";
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = edmResult.INVOICE[0].ID;
                            Result.UUID = edmResult.INVOICE[0].UUID;
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
                        case "QEF":
                            var qefEArsiv = new QEF.ArchiveService();
                            eArsivProtectedValues.doc.SUBE = eArsivProtectedValues.doc.SUBE == "default" ? "DFLT" : eArsivProtectedValues.doc.SUBE;
                            var qefResult = qefEArsiv.EArsivGonder(eArsivProtectedValues.strFatura, Connector.m.VknTckn, EVRAKSN, eArsivProtectedValues.doc.SUBE, eArsivProtectedValues.doc.BaseUBL.IssueDate.Value.Date);

                            if (qefResult.Result.resultText != "İşlem başarılı.")
                                throw new Exception(qefResult.Result.resultText);

                            ByteData = qefResult.Belge.belgeIcerigi;

                            Result.DurumAciklama = qefResult.Result.resultText;
                            Result.DurumKod = qefResult.Result.resultCode;
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                            Result.UUID = qefResult.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
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
                        default:
                            throw new Exception("Tanımlı Entegratör Bulunamadı!");
                    }
                }
                return response;
            }

            // Esle fonksiyonu yok. 
            public override void Esle()
            {
                throw new NotImplementedException();
            }

            public static string Iptal(int EVRAKSN, string EVRAKNO, string GUID, decimal TUTAR)
            {
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                public override string Iptal()
                {

                    base.Iptal();
                    var fitEArsiv = new FIT.ArchiveWebService();
                    var fitResult = fitEArsiv.EArsivIptal(EVRAKNO, TUTAR);
                    if (fitResult.Result.Result1 == ResultType.SUCCESS)
                    {
                        Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, fitResult.invoiceCancellation.message, DateTime.Now);
                    }
                    else
                    {
                        throw new Exception(fitResult.invoiceCancellation.message);
                    }
                    return fitResult.invoiceCancellation.message + " ve İlgili eArşiv Fatura Silinmiştir!";
                }
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
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
            }
            public sealed class EDMEArsiv : EArsiv
            {
                public override string Iptal()
                {
                    base.Iptal();
                    var edmEArsiv = new EDM.ArchiveWebService();
                    var edmpResult = edmEArsiv.IptalFatura(GUID, TUTAR);

                    Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, "", DateTime.Now);

                    return "eArşiv Fatura Başarıyla İptal Edilmiştir ve İlgili eArşiv Fatura Silinmiştir!";
                }
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                public override string Iptal()
                {
                    base.Iptal();
                    var qefEArsiv = new QEF.ArchiveService();
                    var qefpResult = qefEArsiv.IptalFatura(GUID);

                    if (qefpResult.resultText != "İşlem başarılı.")
                        throw new Exception(qefpResult.resultText);

                    Entegrasyon.SilEarsiv(EVRAKSN, TUTAR, qefpResult.resultText, DateTime.Now);

                    return "eArşiv Fatura Başarıyla İptal Edilmiştir ve İlgili eArşiv Fatura Silinmiştir!";
                }
            }
            public static string Itiraz(string EVRAKNO, UBL.ObjectionClass Objection)
            {
            }
            public sealed class FIT_ING_INGBANKEArsiv : EArsiv
            {
                public override string Itiraz()
                {

                    base.Itiraz();
                    var fitEArsiv = new FIT.ArchiveWebService();
                    var fitResult = fitEArsiv.EArsivItiraz(EVRAKNO, Objection);
                    if (fitResult.Result == ResultType.FAIL)
                    {
                        throw new Exception(fitResult.Detail);
                    }
                    return fitResult.Detail;
                }
            }
            public sealed class DPlanetEArsiv : EArsiv
            {
                public override string Itiraz()
                {
                    base.Itiraz();
                    var dpArsiv = new DigitalPlanet.ArchiveWebService();
                    dpArsiv.WebServisAdresDegistir();
                    dpArsiv.EArsivItiraz();
                    return "Test!";

                }
            }
            public sealed class EDMEArsiv : EArsiv
            {
                public override string Itiraz()
                {
                    base.Itiraz();
                    var edmArsiv = new EDM.ArchiveWebService();
                    edmArsiv.WebServisAdresDegistir();
                    edmArsiv.EArsivItiraz();
                    return "Test!";
                }
            }
            public sealed class QEFEEArsiv : EArsiv
            {
                public override string Itiraz()
                {
                    base.Itiraz();
                    //var edmArsiv = new EDM.ArchiveWebService();
                    //edmArsiv.WebServisAdresDegistir();
                    //edmArsiv.EArsivItiraz();
                    return "Test!";
                }
            }

            ///Sadece EArsivde olanlar

            public static void GonderilenGuncelle(int EVRAKSN)

            {
                var Result = new Results.EFAGDN();
                switch (UrlModel.SelectedItem)
                {
                    case "FIT":
                    case "ING":
                    case "INGBANK":
                        var fitArsiv = new FIT.ArchiveWebService();
                        fitArsiv.WebServisAdresDegistir();

                        string UUID = Entegrasyon.GetUUIDFromEvraksn(new List<int> { EVRAKSN })[0];
                        var evrakno = Entegrasyon.GetEvraknoFromEvraksn(new[] { EVRAKSN }.ToList())[0];

                        getSignedInvoiceResponseType ars = null;
                        Exception exx = null;

                        try
                        {
                            ars = fitArsiv.ImzaliIndir(UUID.ToUpper(), evrakno, EVRAKSN);
                        }
                        catch (Exception ex)
                        {
                            exx = ex;
                        }
                        try
                        {
                            ars = fitArsiv.ImzaliIndir(UUID.ToLower(), evrakno, EVRAKSN);
                        }
                        catch (Exception ex)
                        {
                            exx = ex;
                        }

                        if (ars != null)
                        {
                            Result.DurumAciklama = ars.Detail;
                            Result.DurumKod = "150";
                            Result.DurumZaman = DateTime.Now;
                            Result.EvrakNo = ars.invoiceNumber;
                            Result.UUID = ars.UUID;
                            Result.ZarfUUID = "";
                            Result.YanitDurum = 0;

                            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, ars.binaryData, onlyUpdate: true);
                        }
                        else if (exx != null)
                        {
                            throw exx;
                            //Değiştir--CpmMessageBox.Show($"Entegratörden Fatura Bilgisi Dönmedi.\nEvraksn:{EVRAKSN}\nEvrakno:{evrakno}\nUUID:{UUID}", "Dikkat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                            //Değiştir--CpmMessageBox.Show($"Entegratör Açıklaması: {exx.Message}", "Dikkat", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                        }

                        break;
                    case "DPLANET":
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
                    case "EDM":
                        var edmArsiv = new EDM.ArchiveWebService();
                        var edmGelen = edmArsiv.EArsivIndir(Entegrasyon.GetUUIDFromEvraksn(new List<int> { EVRAKSN })[0]);

                        if (edmGelen.Length > 0)
                        {
                            Result.DurumAciklama = edmGelen[0].HEADER.STATUS_DESCRIPTION;
                            Result.DurumKod = "";
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.EvrakNo = edmGelen[0].ID;
                            Result.UUID = edmGelen[0].UUID;
                            Result.ZarfUUID = edmGelen[0].HEADER.ENVELOPE_IDENTIFIER ?? "";
                            Result.YanitDurum = 0;

                            Entegrasyon.UpdateEfagdn(Result, EVRAKSN, edmGelen[0].CONTENT.Value, onlyUpdate: true);

                            Result.DurumAciklama = edmGelen[0].HEADER.RESPONSE_CODE == "REJECT" ? "" : "";
                            Result.DurumKod = edmGelen[0].HEADER.RESPONSE_CODE == "REJECT" ? "3" : (edmGelen[0].HEADER.RESPONSE_CODE == "ACCEPT" ? "2" : "");
                            Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                            Result.UUID = edmGelen[0].UUID;

                            Entegrasyon.UpdateEfagdnStatus(Result);

                            //File.WriteAllText("edm.json", JsonConvert.SerializeObject(edmGelen));

                            if (edmGelen[0].HEADER.EARCHIVE_REPORT_UUID != null)
                                Entegrasyon.UpdateEfagdnGonderimDurum(edmGelen[0].UUID, 4);
                            else if (edmGelen[0].HEADER.STATUS_DESCRIPTION == "SUCCEED")
                                Entegrasyon.UpdateEfagdnGonderimDurum(edmGelen[0].UUID, 3);
                        }
                        else
                            throw new Exception(EVRAKSN + " Seri Numaralı Evrak Gönderilenler Listesinde Bulunmamaktadır!");
                        break;
                    case "QEF":
                        var qefArsiv = new QEF.ArchiveService();
                        var qefGelen = qefArsiv.EArsivIndir(Entegrasyon.GetUUIDFromEvraksn(new List<int> { EVRAKSN })[0]);

                        Result.DurumAciklama = qefGelen.Result.resultText;
                        Result.DurumKod = qefGelen.Result.resultCode;
                        Result.DurumZaman = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Second, 0);
                        Result.EvrakNo = qefGelen.Result.resultExtra.First(elm => elm.key.ToString() == "faturaNo").value.ToString();
                        Result.UUID = qefGelen.Result.resultExtra.First(elm => elm.key.ToString() == "uuid").value.ToString();
                        Result.ZarfUUID = "";
                        Result.YanitDurum = 0;

                        Entegrasyon.UpdateEfagdn(Result, EVRAKSN, qefGelen.Belge.belgeIcerigi, onlyUpdate: true);

                        //if (qefGelen[0].HEADER.EARCHIVE_REPORT_UUID != null)
                        //    Entegrasyon.UpdateEfagdnGonderimDurum(qefGelen[0].UUID, 4);
                        //else if (qefGelen[0].HEADER.STATUS_DESCRIPTION == "SUCCEED")
                        //    Entegrasyon.UpdateEfagdnGonderimDurum(qefGelen[0].UUID, 3);
                        //else
                        //    throw new Exception(EVRAKSN + " Seri Numaralı Evrak Gönderilenler Listesinde Bulunmamaktadır!");
                        break;
                    default:
                        throw new Exception("Tanımlı Entegratör Bulunamadı!");
                }
            }
        }
}
