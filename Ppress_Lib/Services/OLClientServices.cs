using Ppress_Lib;
using Ppress_Lib.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ppress_Client
{
    public class OLClientServices
    {
        private OLAuthentication? _authentication = null;
        private OLDataMapping? _dataMapping = null;
        private OLContentCreation? _contentCreation = null;
        private OLFileStore? _fileStore = null;
        private OLJobCreation? _jobCreation = null;
        private OLOutputCreation? _outputCreation = null;
        private readonly OLClient client;

        /// <summary>
        /// Anthentication service
        /// </summary>
        public OLAuthentication Authentication { 
            get { 
                _authentication ??= new OLAuthentication(client);
                return _authentication; 
            } 
        }

        /// <summary>
        /// DataMapping service
        /// </summary>
        public OLDataMapping DataMapping
        {
            get
            {
                _dataMapping ??= new OLDataMapping(client);
                return _dataMapping;
            }
        }

        /// <summary>
        /// ContentCreation service
        /// </summary>
        public OLContentCreation ContentCreation
        {
            get
            {
                _contentCreation ??= new OLContentCreation(client);
                return _contentCreation;
            }
        }

        /// <summary>
        /// JobCreation service
        /// </summary>
        public OLJobCreation JobCreation
        {
            get
            {
                _jobCreation ??= new OLJobCreation(client);
                return _jobCreation;
            }
        }


        /// <summary>
        /// OutputCreation service
        /// </summary>
        public OLOutputCreation OutputCreation
        {
            get
            {
                _outputCreation ??= new OLOutputCreation(client);
                return _outputCreation;
            }
        }

        /// <summary>
        /// FileStore service
        /// </summary>
        public OLFileStore FileStore
        {
            get
            {
                _fileStore ??= new OLFileStore(client);
                return _fileStore;
            }
        }

        public OLClientServices(OLClient client)
        {
            this.client = client;
        }
    }
}
