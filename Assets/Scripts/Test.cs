using Pixalate.Mobile;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteAlways]
public class Test : MonoBehaviour {
    struct TestStruct {
        public string name;
        public int something;
	}
    
    [ContextMenu( "Test Blocking" )]
    public void TestBlocking () {
        if( !PixalateBlocking.initialized ) {
            PixalateBlocking.Initialize( new BlockingConfig.Builder( "bK11GeegpE6McF49Jh2iUfqsixSI4K1s" ).Build() );
            PixalateBlocking.logLevel = LogLevel.Error;
        }

        PixalateBlocking.PerformBlockingRequest( BlockingMode.Default, ( block, error ) => {
            Debug.Log( block + " " + error );
        });
	}
}
