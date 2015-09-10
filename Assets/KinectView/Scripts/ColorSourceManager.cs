using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System;



public class Point{

	public double x;
	public double y;
	public double r;
	public double g;
	public double b; 

	public Point(double x, double y, double r, double g, double b){

		this.x = x;
		this.y = y;
		this.r = r;
		this.g = g;
		this.b = b; 
	
	}

}

public class ColorSourceManager : MonoBehaviour 
{
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }
	public GameObject DepthSourceManager;
	private DepthSourceManager _DepthManager;
    private KinectSensor _Sensor;
    private ColorFrameReader _Reader;
    private Texture2D _Texture;
    private byte[] _Data;
	private static double _allowedDistance = 0.07; //för bollens rgb(0.5,0.5,0) funkade idag typ runt kl 14:52 (mtp ljuset)

	public Point yellowBall = new Point(0,0,1,1,0.6);
	public double compareRed;
	public double compareGreen;
	public double compareBlue;
	public double currentColorDistance;
	public Point tempPixelColor = new Point(0,0,0,0,0);
    
    public Texture2D GetColorTexture()
    {
        return _Texture;
    }
	 
	public static double Euclidean(Point p1, Point p2)
	{
		return Math.Sqrt(Math.Pow(p1.r - p2.r, 2) + Math.Pow(p1.g - p2.g, 2) + Math.Pow(p1.b - p2.b, 2));
	}
    
    void Start()
    {
        _Sensor = KinectSensor.GetDefault();
        
        if (_Sensor != null) 
        {
            _Reader = _Sensor.ColorFrameSource.OpenReader();
            
            var frameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = frameDesc.Width;
            ColorHeight = frameDesc.Height;
            
            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];

            
            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        	
		}
    }
    
    void Update () 
    {
        if (_Reader != null) 
        {
            var frame = _Reader.AcquireLatestFrame();
            
            if (frame != null)
            {
                frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                _Texture.LoadRawTextureData(_Data);
                _Texture.Apply();
                
                frame.Dispose();
                frame = null;

				//Rasmus här är nya koden för sökningen av bollen. Vi har globala variabler uppe som vi använder
				//Point är bara en punkt som innehåller x,y (koordinater) och r,g,b (rgb-värden). tempPixelColor och yellowBall är Pointobjekt
				for(int i = 0; i < ColorWidth; i+=1){
					for(int j = 0; j < ColorHeight; j+=1){
						tempPixelColor.r = _Texture.GetPixel(i,j).r;
						tempPixelColor.g = _Texture.GetPixel(i,j).g;
						tempPixelColor.b = _Texture.GetPixel(i,j).b;

						//Euclidean räknar bara ut euklidiska distansen i rgb-format mellan pixeln vi kollar och vårt försatta värde av den gula bollen
						currentColorDistance = Euclidean(tempPixelColor, yellowBall);
						if(currentColorDistance < _allowedDistance){
							//Debug.Log (i);

							// _DepthManager borde innehålla djupdatan men om man kommenterar ut nedstående tre rader så funkar ej utskriften av pixelvärdet längre???
							if (DepthSourceManager == null){return;}
							
							_DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
                            
                            if (_DepthManager == null){return;}
							//Debug.Log (_DepthManager.GetData());
							//*Debug.Log(currentColorDistance);

						}
					}
				}
				//Debug.Log (ColorWidth);
				//Debug.Log (ColorHeight);
			}
        }
    }

    void OnApplicationQuit()
    {
        if (_Reader != null) 
        {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null) 
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }
            
            _Sensor = null;
        }
    }
}
