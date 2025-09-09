using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace codecrafters_sqlite.src
{
    internal class Page
    {

        internal Page(File databaseFile, int pageNumber)
        {
            int offset = 0;
            if (pageNumber == 1) offset = 100;
            else offset = (pageNumber - 1) * databaseFile.pageSize;
            
                

        }
    }
}
