using System;
using System.Collections.Generic;
using System.Text;

namespace BuildsTriggeredPerUser
{
    public class models
    {
        public class inputCheck
        {
            public Boolean failed { get; set; }
            public string Field { get; set; }
            public string HowFailed { get; set; }
        }

        public class project
        {
            public List<projectDetails> value { get; set; }
        }

        public class projectDetails
        {
            public string id { get; set; }
            public string name { get; set; }
            public string state { get; set; }
        }

        public class build
        {
            public List<buildDetails> value { get; set; }
        }

        public class buildDetails
        {
            public string id { get; set; }
            public userDetails requestedFor { get; set; }
            public repoDetails repository { get; set; }
            public queueDetails queue { get; set; }
            public DateTime queueTime { get; set; }
            public string theproject { get; set; }
        }

        public class userDetails
        {
            public string uniqueName { get; set; }
        }

        public class repoDetails
        {
            public string name { get; set; }
            public string type { get; set; }
        }

        public class queueDetails
        {
            public string name { get; set; }
        }

        public class uploadAttachmentResponse
        {
            public string ID { get; set; }
            public string url { get; set; }
        }

        public class workItemLink
        {
            public string rel { get; set; }
            public string url { get; set; }
            public attribute attributes { get; set; }
        }

        public class attribute
        {
            public string comment { get; set; }
            public attribute(string comment)
            {
                this.comment = comment;
            }
        }
    }
}
