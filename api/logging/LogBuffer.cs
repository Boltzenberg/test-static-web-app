using System.Text;
using Boltzenberg.Functions.Comms;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Boltzenberg.Functions.Logging
{
    public class LogBuffer
    {
        private StringBuilder sbLog;
        private bool writeOnClose;

        public LogBuffer(string heading)
            : this(heading, false)
        {}

        public LogBuffer(string heading, bool writeOnClose)
        {
            this.writeOnClose = writeOnClose;
            this.sbLog = new StringBuilder(heading);
            this.sbLog.AppendLine();
        }

        public void Info(string format, params object[] args)
        {
            this.Info(string.Format(format, args));
        }

        public void Info(string line)
        {
            this.sbLog.AppendLine(string.Format("*INFO* — `{0}`: {1}", DateTime.UtcNow.ToString("o"), line));
            this.writeOnClose = true;
        }

        public void Error(string format, params object[] args)
        {
            this.Error(string.Format(format, args));
        }

        public void Error(string line)
        {
            this.sbLog.AppendLine(string.Format("*ERROR* — `{0}`: {1}", DateTime.UtcNow.ToString("o"), line));
            this.writeOnClose = true;
        }

        public void Exception(Exception ex)
        {
            this.sbLog.AppendLine(string.Format("*EXCEPTION* - `{0}`: {1}", DateTime.UtcNow.ToString("o"), ex.ToString()));
            this.writeOnClose = true;
        }

        public async Task Close()
        {
            if (this.writeOnClose)
            {
                await Telegram.LogAsync(this.sbLog.ToString());
            }
        }

        public static async Task<IActionResult> Wrap(string operationName, HttpRequest req, Func<HttpRequest, LogBuffer, Task<IActionResult>> wrapped)
        {
            LogBuffer log = new LogBuffer(operationName);

            try
            {
                return await wrapped(req, log);
            }
            catch (Exception ex)
            {
                log.Exception(ex);
                throw;
            }
            finally
            {
                await log.Close();
            }
        }
    }
}
