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
	private static double _allowedDistance = 0.10; //för bollens rgb(0.5,0.5,0) funkade idag typ runt kl 14:52 (mtp ljuset)
	
	//green-ish ball: 0.465, 0.680, 0.164
	//bloodred circle paper thingy 0.664, 0.207, 0.082
	//public Point yellowBall = new Point(0,0,  0.465, 0.680, 0.164);
	public Point yellowBall = new Point(0,0,  0.664, 0.207, 0.082);
	public double compareRed;
	public double compareGreen;
	public double compareBlue;
	public double currentColorDistance;
	public Point tempPixelColor = new Point(0,0,0,0,0);
	public LineRenderer lineRenderer;

	public double prevX, prevY;
	public int offsetX, offsetY, startSearchOffsetX, startSearchOffsetY;

	public void DrawCircle(double x, double y){
		x /= 75.0;
		y /= -75.0;
		x -= 10;
		y += 7;

		lineRenderer.material = new Material (Shader.Find ("Particles/Additive"));
		lineRenderer.SetColors (Color.red, Color.red);
		lineRenderer.SetWidth(0.2F, 0.2F);

		lineRenderer.SetVertexCount(5);
		double r = 1.5F;
		lineRenderer.SetPosition(0, new Vector3((float)x, (float)y, 0));
		lineRenderer.SetPosition(1, new Vector3((float)(x+r), (float)y, 0));
		lineRenderer.SetPosition(2, new Vector3((float)(x+r), (float)(y+r), 0));
		lineRenderer.SetPosition(3, new Vector3((float)x, (float)(y+r), 0));
		lineRenderer.SetPosition(4, new Vector3((float)x, (float)y, 0));
	
	}
    
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
			prevX = -1;
			prevY = -1;
			offsetX = -250;
			offsetY = -250;

            _Reader = _Sensor.ColorFrameSource.OpenReader();
			lineRenderer = gameObject.AddComponent<LineRenderer>();
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

				if (prevX != -1 && prevY != -1) {
					startSearchOffsetX = (int)prevX;
					startSearchOffsetY = (int)prevY;
				} else {
					startSearchOffsetX = 0;
					startSearchOffsetY = 0;
				}

				//Rasmus här är nya koden för sökningen av bollen. Vi har globala variabler uppe som vi använder
				//Point är bara en punkt som innehåller x,y (koordinater) och r,g,b (rgb-värden). tempPixelColor och yellowBall är Pointobjekt
				int pixelSteps = 10;
				for(int i = 0; i < ColorWidth; i+=pixelSteps){
					for(int j = 0; j < ColorHeight; j+=pixelSteps){
						//int ii = (offsetX + startSearchOffsetX + i) % ColorWidth;
						//int jj = (offsetY + startSearchOffsetY + j) % ColorHeight;

						int ii = i;
						int jj = j;

						tempPixelColor.r = _Texture.GetPixel(ii,jj).r;
						tempPixelColor.g = _Texture.GetPixel(ii,jj).g;
						tempPixelColor.b = _Texture.GetPixel(ii,jj).b;

						//Euclidean räknar bara ut euklidiska distansen i rgb-format mellan pixeln vi kollar och vårt försatta värde av den gula bollen
						currentColorDistance = Euclidean(tempPixelColor, yellowBall);
						if(currentColorDistance < _allowedDistance){
							DrawCircle(ii,jj);
							//Debug.Log("ii/jj is:" + ii.ToString() + "/" + jj.ToString());

							//We have found what we think is a match. Now find the middle point

							int precisionCheckSize = 60;
							int numMatches = 0;
							double x = 0, y = 0;
							double xSum = 0, ySum = 0;
							for (int i2 = -precisionCheckSize; i2 < precisionCheckSize; i2++) {
								for (int j2 = -precisionCheckSize; j2 < precisionCheckSize; j2++) {
									x = ii+i2;
									y = jj+j2;
									//Debug.Log("INSIDE CHECK: ii/jj is:" + ii.ToString() + "/" + jj.ToString());


									if (x >= 0 && x < ColorWidth && y >= 0 && y < ColorHeight) {
										tempPixelColor.r = _Texture.GetPixel((int)x, (int)y).r;
										tempPixelColor.g = _Texture.GetPixel((int)x, (int)y).g;
										tempPixelColor.b = _Texture.GetPixel((int)x, (int)y).b;


										/*
										if (Euclidean(tempPixelColor, yellowBall) < 0.3) {
											Debug.Log("euclidian: " + Euclidean(tempPixelColor, yellowBall).ToString());
										}*/

										if (Euclidean(tempPixelColor, yellowBall) < _allowedDistance*0.7) {
											//match was found!
											//Debug.Log("Match found!");
											numMatches += 1;
											xSum += x;
											ySum += y;
										}
									}
								}
							}

							if (numMatches > 0) {
								//Debug.Log(numMatches);
								x = xSum / (double)numMatches;
								y = ySum / (double)numMatches;

								if (prevX != -1 && prevY != -1) {
									DrawCircle((x+prevX)/2, (y+prevY)/2);
								} else {
									DrawCircle(x, y);
								}
								prevX = x;
								prevY = y;

								Debug.Log ("x=" + x.ToString() + ", y=" + y.ToString() + "numMatches=" + numMatches.ToString());


								//jump out of loop, don't look for more objects
								i = ColorWidth + 9001;
								j = ColorHeight + 9001;
							}

							// _DepthManager borde innehålla djupdatan men om man kommenterar ut nedstående tre rader så funkar ej utskriften av pixelvärdet längre???
							if (DepthSourceManager == null){return;}
							
							_DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
                            
                            if (_DepthManager == null){return;}
							//Debug.Log (_DepthManager.GetData());
							//*Debug.Log(currentColorDistance);

						} else {
							//DrawCircle(-500.0,-500.0);
							//lineRenderer.SetVertexCount(0);
							prevX = -1;
							prevY = -1;
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
