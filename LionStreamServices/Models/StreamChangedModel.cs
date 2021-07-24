using System;
using System.Collections.Generic;
using System.Text;

namespace LionStreamServices.Models
{
    class StreamChangedModel
    {

        public string Id { get; set; }
        public string User_id { get; set; }
        public string User_login { get; set; }
        public string User_name { get; set; }
        public string Game_id { get; set; }
        public string Game_name { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public int Viewer_count { get; set; }
        public DateTime Started_at { get; set; }
        public string Language { get; set; }
        public string Thumbnail_url { get; set; }
        public object[] Tag_ids { get; set; }


    }
}
