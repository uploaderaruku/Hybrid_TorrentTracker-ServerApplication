using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Tracker_Server
{
    public class AdminPageController : ControllerBase
    {
        [HttpGet("admin")] //=> http://localhost/admin
        public async Task<IActionResult> AdminPage(int key, int no_peer_id, int compact)
        {
            

            return NoContent();
        }

        [HttpGet("stats")] //=> http://localhost/stats
        public async Task<IActionResult> StatPage()
        {
            TorrentTrackerServer.RefreshStatisticsInfo();
            var stats = TorrentTrackerServer.GetStatisticsInfo();

            return new ContentResult(){Content = stats , ContentType = "text/plain"};
        }
    }
}
