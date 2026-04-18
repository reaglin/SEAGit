using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEAGit.Models
{
    public class GitRepository
    {
        public string Name { get; set; }
        public string LocalPath { get; set; }

        // Storing the date helps users know when they last pushed their changes
        public DateTime LastPublished { get; set; }
    }
}
