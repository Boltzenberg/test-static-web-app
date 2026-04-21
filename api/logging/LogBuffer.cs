using System.ClientModel.Primitives;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using Boltzenberg.Functions.Comms;
using Boltzenberg.Functions.Storage;
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
            this.sbLog.AppendLine(string.Format("*EXCEPTION* - `{0}`: {1}: {2}", DateTime.UtcNow.ToString("o"), ex.GetType(), ex.Message));
            this.writeOnClose = true;
        }

        public void OperationResult<T>(string message, OperationResult<T> result)
        {
            if (result.Code == ResultCode.Success)
            {
                // Do nothing
            }
            else if (result.Code == ResultCode.PreconditionFailed)
            {
                this.Error("{0} - PreconditionFailed");
            }
            else if (result.Code == ResultCode.GenericError)
            {
                if (result.Error != null)
                {
                    this.Error("{0} - {1}", message, result.Error.ToString());
                }
                else
                {
                    this.Error("{0}", message);
                }
            }
        }

        public async Task Close()
        {
            if (this.writeOnClose)
            {
                await Telegram.LogAsync(this.sbLog.ToString());
            }
        }

        public static async Task<IActionResult> Wrap(string operationName, HttpRequest req, Func<HttpRequest, LogBuffer, Task<IActionResult>> wrapped, bool writeOnClose = false)
        {
            LogBuffer log = new LogBuffer(operationName, writeOnClose);

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
