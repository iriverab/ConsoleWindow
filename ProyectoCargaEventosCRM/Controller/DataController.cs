using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ProyectoCargaEventosCRM.Controller
{
    public class DataController
    {
        private Fichas20Entities db = new Fichas20Entities();
        internal void ProcesarEventos()
        {
            var _ListadoRegistros = db.CRM_Registra_actividades_para_enviar_a_CRM.Where(x => x.Estado == "0").ToList().Take(1);
            string _cabecera = CreaCabecera();
            string _pie = CreaPie();
            string _cuerpo = "", _psXmlUnitario = "", _TokenApi = "";
            int _IdEvento = 0;
            foreach (var obj in _ListadoRegistros)
            {
                _cuerpo = CreateXMLString(obj);
                _psXmlUnitario = string.Format("{0}{1}{2}", _cabecera, _cuerpo, _pie); //XML FINAL
                //Grabamos registro en CRM
                _IdEvento = Convert.ToInt32(db.CRM_Eventos_Insert(_psXmlUnitario, "CreaTestEnsayoCharla", "", "Task.Net").First());
                _TokenApi = LogIntoApiCepech();

                if (_TokenApi == "")
                    db.CRM_Eventos_Cerrar(_IdEvento, "ERROR EN LOGIN");
                else
                {
                    string psSalida = ConsumirMetodoFinal(_IdEvento, _TokenApi);
                    if (psSalida == "")
                        db.CRM_Eventos_Cerrar(_IdEvento, "ERROR en consumo de API WiredToCRM");
                }
                ActualizarRegistro(obj.Id_reg);
                Console.Write(string.Format("{0}", _psXmlUnitario));
            }
            Console.ReadKey();
        }

        private string ConsumirMetodoFinal(int idEvento, string tokenApi)
        {
            string psOut = EjecutarApi(string.Format("{0}{1}?Id={2}", ConfigurationManager.AppSettings["Ruta"].ToString(), "Cepech/WiredToCrm", idEvento.ToString()), JsonConvert.SerializeObject(new { usuario = "CRMCpech", password = "28zF02nXPVThAEAY" }), "GET", tokenApi);
            JObject jobject = JObject.Parse(psOut);
            if (jobject["codigo"].ToString() == "0")
                return jobject["mensaje"].ToString();
            else
                return "";
        }

        private string LogIntoApiCepech()
        {
            string psOut = EjecutarApi(string.Format("{0}{1}", ConfigurationManager.AppSettings["Ruta"].ToString(), "Token/getToken"), JsonConvert.SerializeObject(new { usuario = "CRMCpech", password = "28zF02nXPVThAEAY" }), "POST", "");
            JObject jobject = JObject.Parse(psOut);
            if (jobject["estado"].ToString() == "1")
                return jobject["token"].ToString();
            else
                return "";
        }

        /// <summary>
        /// Metodo que permite ejecutar una api con los parametros 
        /// </summary>
        /// <param name="Ruta">Ruta final del endpoint</param>
        /// <param name="Data">Data en formato json convertida en String</param>
        /// <param name="Method">Enumerado de metodo de innvocación</param>
        /// <returns>string con json callback from endpoint</returns>
        private string EjecutarApi(string Ruta, string Data, string Method, string Token = "")
        {
            string psSalida = "";
            using (HttpClient httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                if (Token != "")
                    httpClient.DefaultRequestHeaders.Add("Authorization", string.Format("bearer {0}", Token));
                ServicePointManager.Expect100Continue = false;
                HttpResponseMessage messge = null;
                try
                {
                    if (Method == "POST")
                    {
                        HttpContent content = new StringContent(Data, Encoding.UTF8, "application/json");
                        messge = httpClient.PostAsync(Ruta, content).Result;
                    }
                    else
                        messge = httpClient.GetAsync(Ruta).Result;

                    if (messge.IsSuccessStatusCode)
                    {
                        string result = messge.Content.ReadAsStringAsync().Result;
                        psSalida = result;
                    }
                    else
                        psSalida = "";
                }
                catch (Exception ex)
                {
                    if (messge == null)
                        messge = new HttpResponseMessage();
                    messge.StatusCode = HttpStatusCode.InternalServerError;
                    messge.ReasonPhrase = string.Format("RestHttpClient.SendRequest failed: {0}", ex.Message);
                    psSalida = "";
                }
            }
            return psSalida;
        }

        private void ActualizarRegistro(int Id)
        {
            db.CRM_RegistroUpdate(Id);
        }

        private string CreaPie()
        {
            StringBuilder xml = new StringBuilder();
            xml.Append(@"</data>");
            xml.Append(@"</tem:data>");
            xml.Append(@"</tem:CreaTestEnsayoCharla>");
            xml.Append(@"</soapenv:Body>");
            xml.Append(@"</soapenv:Envelope>");
            return xml.ToString();
        }

        private string CreaCabecera()
        {
            StringBuilder xml = new StringBuilder();
            xml.Append(@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:tem=""http://tempuri.org/"">");
            xml.Append(@"<soapenv:Header/>");
            xml.Append(@"<soapenv:Body>");
            xml.Append(@"<tem:CreaTestEnsayoCharla>");
            xml.Append(@"<tem:data>");
            xml.Append(@"<data>");

            return xml.ToString();
        }

        private string CreateXMLString(CRM_Registra_actividades_para_enviar_a_CRM obj)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append(@"<Actividad>");
            xml.Append(@"<Rut>" + obj.Rut.ToString().Trim() + "</Rut>");
            xml.Append(@"<Dv>" + obj.digito_dv.ToString().Trim() + "</Dv>");
            xml.Append(@"<Nombre>" + obj.Nombre_actividad.ToString().Trim() + "</Nombre>");
            xml.Append(@"<Tipo>" + obj.Tipo_actividad.ToString().Trim() + "</Tipo>");
            xml.Append(@"<Puntaje>" + obj.puntaje.ToString().Trim() + "</Puntaje>");
            xml.Append(@"</Actividad>");
            return xml.ToString();
        }
    }
}
