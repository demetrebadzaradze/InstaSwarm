using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using System;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace InstaSwarm.services
{
    // <summary>
    //    Design Goals for InstagramAgent
    //    Coordinate Workflow: Manage the process of receiving video links, downloading, storing, and uploading to Instagram accounts.
    //    Queue Management: Handle a queue of videos(initially using Instagram DMs, with potential SQLite integration).
    //    File Management: Interface with the YtDlp class to download videos and store them in the./videos directory.
    //    API Integration: Use the InstagramClient to upload videos to multiple Instagram accounts.
    //    Moderation: Restrict video submissions to approved contributors and allow the owner to approve/reject videos.
    //    Extensibility: Support future expansion to TikTok/YouTube and a dashboard.
    //    Error Handling: Robustly handle failures (e.g., invalid links, API limits, download errors).
    // </summary>
    public class InstagramAgent
    {
        public List<InstagramClient> Clients { get; set; }
        public InstagramAgent()
        {

        }
    }
}
