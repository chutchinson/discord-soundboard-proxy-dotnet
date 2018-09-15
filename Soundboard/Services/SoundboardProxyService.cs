using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Soundboard.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Soundboard.Services
{
    public class SoundboardProxyService
    {
        private readonly HttpClient _http;
        private readonly ILogger _logger;
        private SoundboardOptions _options;

        public SoundboardProxyService(
            ILoggerFactory loggerFactory,
            IOptionsSnapshot<SoundboardOptions> options,
            IOptionsMonitor<SoundboardOptions> optionsMonitor)
        {
            _logger = loggerFactory.CreateLogger<SoundboardProxyService>();
            _http = new HttpClient();
            _options = options.Value;

            optionsMonitor.OnChange((cfg, _) =>
            {
                _options = cfg;
            });
        }

        public async Task SendCommandAsync(string command)
        {
            if (string.IsNullOrWhiteSpace(_options.CommandUri))
            {
                _logger.LogError("Soundboard proxy command URI not configured.");
                return;
            }

            try
            {
                _logger.LogInformation("Sending proxy command <{0}>...", command);

                var uri = string.Format(_options.CommandUri, command);
                await _http.PostAsync(uri, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send proxy command <{0}>: {1}",
                    command, ex.Message);
            }
            finally
            {
                _logger.LogTrace("Sent proxy command <{0}>", command);
            }
        }
    }
}
