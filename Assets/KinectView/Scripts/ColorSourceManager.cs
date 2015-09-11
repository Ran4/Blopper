using UnityEngine;
using System.Collections;
using Windows.Kinect;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Point
{
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
    public LineRenderer lineRenderer;

    public double prevX, prevY;
    public double foundX, foundY;
    //SOCKET
    public static NetworkStream stream;
    byte[] dataToSend;

    public void DrawBox(double x, double y, Color col){
        //Rescale x and y to better fit on the screen
        x /= 75.0;
        y /= -75.0;
        x -= 10;
        y += 7;

        lineRenderer.material = new Material (Shader.Find ("Particles/Additive"));
        //lineRenderer.SetColors (Color.red, Color.red);
        lineRenderer.SetColors (col, col);
        lineRenderer.SetWidth(0.2F, 0.2F);

        lineRenderer.SetVertexCount(5);
        double r = 1.5F;
        lineRenderer.SetPosition(0, new Vector3((float)x, (float)y, 0));
        lineRenderer.SetPosition(1, new Vector3((float)(x+r), (float)y, 0));
        lineRenderer.SetPosition(2, new Vector3((float)(x+r), (float)(y+r), 0));
        lineRenderer.SetPosition(3, new Vector3((float)x, (float)(y+r), 0));
        lineRenderer.SetPosition(4, new Vector3((float)x, (float)y, 0));
    }
    
    public Texture2D GetColorTexture() {
        return _Texture;
    }
     
    public static double Euclidean(Point p1, Point p2) {
        return Math.Sqrt(Math.Pow(p1.r - p2.r, 2) + Math.Pow(p1.g - p2.g, 2) + Math.Pow(p1.b - p2.b, 2));
    }
    
    public static bool isColorCloseEnough(double r, double g, double b, Point other, double dist) {
        return Math.Sqrt(Math.Pow(r - other.r, 2) + Math.Pow(g - other.g, 2) + Math.Pow(b - other.b, 2)) < dist;
    }
    
    void Start()
    {
        //Socket
        Connect ("127.0.0.1", "Hej");
        //SOCKET: data ska vara koordinater
        //byte[] data = System.Text.Encoding.ASCII.GetBytes("hej3");
        //stream.Write(data, 0, data.Length);
        _Sensor = KinectSensor.GetDefault();
        
        if (_Sensor != null) {
            prevX = -1;
            prevY = -1;

            _Reader = _Sensor.ColorFrameSource.OpenReader();
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            var frameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
            ColorWidth = frameDesc.Width;
            ColorHeight = frameDesc.Height;
            
            _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
            _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];

            
            if (!_Sensor.IsOpen) {
                _Sensor.Open();
            }
        }
    }
    
    void Update () {
        if (_Reader == null) {
            continue;
        }
        
        var frame = _Reader.AcquireLatestFrame();
        
        if (frame == null) {
            continue;
        }
        
        frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
        _Texture.LoadRawTextureData(_Data);
        _Texture.Apply();
        
        frame.Dispose();
        frame = null;

        //Rasmus här är nya koden för sökningen av bollen. Vi har globala variabler uppe som vi använder
        //Point är bara en punkt som innehåller x,y (koordinater) och r,g,b (rgb-värden). tempPixelColor och yellowBall är Pointobjekt
        int pixelSteps = 7;
        for (int i = 0; i < ColorWidth; i+=pixelSteps){
            for (int j = 0; j < ColorHeight; j+=pixelSteps){
                int ii = i;
                int jj = j;

                double r = _Texture.GetPixel(i,j).r;
                double g = _Texture.GetPixel(i,j).g;
                double b = _Texture.GetPixel(i,j).b;

                //Euclidean räknar bara ut euklidiska distansen i rgb-format mellan pixeln vi kollar och vårt försatta värde av
                //den gula bollen
                //if (Euclidean(tempPixelColor, yellowBall) < _allowedDistance) {
                if (isColorCloseEnough(r,g,b, yellowBall, _allowedDistance)) {
                    //We have found what we think is a match. Now find the middle point
                    DrawBox(i, j, Color.red);

                    int precisionCheckSize = 200;
                    int numMatches = 0;
                    double x = 0, y = 0;
                    double xSum = 0, ySum = 0;
                    for (int i2 = -precisionCheckSize; i2 < precisionCheckSize; i2 += 4) {
                        for (int j2 = -precisionCheckSize; j2 < precisionCheckSize; j2 += 4) {
                            x = i + i2;
                            y = j + j2;
                            
                            if (x >= 0 && x < ColorWidth && y >= 0 && y < ColorHeight) {
                                r = _Texture.GetPixel((int)x, (int)y).r;
                                g = _Texture.GetPixel((int)x, (int)y).g;
                                b = _Texture.GetPixel((int)x, (int)y).b;
                                
                                //if (Euclidean(tempPixelColor, yellowBall) < _allowedDistance*0.7) {
                                if (isColorCloseEnough(r, g, b, yellowBall, _allowedDistance*0.7)) {
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

                        //SOCKET: data ska vara koordinater
                        //dataToSend = System.Text.Encoding.ASCII.GetBytes("hej2");
                        dataToSend = System.Text.Encoding.ASCII.GetBytes(x.ToString() + " " + y.ToString());
                        if (stream != null) { stream.Write(dataToSend, 0, dataToSend.Length); }

                        if (prevX != -1 && prevY != -1) { //smooth it out if we got a match previous frame
                            DrawBox((x+prevX)/2, (y+prevY)/2, Color.red);
                        } else { //we don't have a previous position, so just print it at the current position
                            DrawBox(x, y, Color.red);
                        }
                        prevX = x;
                        prevY = y;
                        foundX = x;
                        foundY = y;

                        //Debug.Log ("x=" + x.ToString() + ", y=" + y.ToString() + "numMatches=" + numMatches.ToString());
                        Debug.Log ("numMatches: " + numMatches.ToString());

                        //jump out of loop, don't look for more objects. Yes, this is terrible.
                        i = ColorWidth + 9001;
                        j = ColorHeight + 9001;
                    }

                    // _DepthManager borde innehålla djupdatan men om man kommenterar ut nedstående tre rader så funkar ej utskriften
                    // av pixelvärdet längre???
                    if (DepthSourceManager == null){return;}
                    
                    _DepthManager = DepthSourceManager.GetComponent<DepthSourceManager>();
                    
                    if (_DepthManager == null){return;}
                    //Debug.Log (_DepthManager.GetData());

                } else {
                    //DrawBox(-500.0,-500.0, Color.red);
                    //lineRenderer.SetVertexCount(0);
                    prevX = -1;
                    prevY = -1;
                }
            }
        }
    }
    
    //SOCKET
    static void Connect(String server, String message)
    {
        try {
            Int32 port = 12345;
            TcpClient client = new TcpClient(server, port);
            
            // Translate the passed message into ASCII and store it as a Byte array.
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            
            stream = client.GetStream();
            
            // Send the message to the connected TcpServer.
            stream.Write(data, 0, data.Length);
            data = System.Text.Encoding.ASCII.GetBytes("hej2");
            stream.Write(data, 0, data.Length);

            //stream.Close();
            //client.Close();
        }
        catch (ArgumentNullException e) {
            Console.WriteLine("ArgumentNullException: {0}", e);
        }
        catch (SocketException e) {
            Console.WriteLine("SocketException: {0}", e);
        }
        
        Console.WriteLine("\n Press Enter to continue...");
        Console.Read();
    }

    void OnApplicationQuit()
    {
        if (_Reader != null) {
            _Reader.Dispose();
            _Reader = null;
        }
        
        if (_Sensor != null) {
            if (_Sensor.IsOpen) {
                _Sensor.Close();
            }
            _Sensor = null;
        }
    }
}
