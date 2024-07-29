using System;
using System.Numerics;
using ScottPlot;

class Program
{

    // Use seed in constructor to get reproducible results
    private static Random rand = new Random(42);

    static void Main(string[] args)
    {
        KalmanFilter filter = new KalmanFilter(0.1f, 0.1f, 1.0f);

        // Arrays to plot eventually
        List<double> trueX_List = [];
        List<double> trueY_List = [];

        List<double> measuredX_List = [];
        List<double> measuredY_List = [];

        List<double> estimatedX_List = [];
        List<double> estimatedY_List = [];


        // Simulate some noisy measurements
        for (int i = 0; i < 100; i++)
        {
            // ===================================================================
            // Noisy measurements simulated here
            // In real application, replace this with x and y values from sensor
            // ===================================================================

            // // Uncomment below for linear motion
            // float trueX = i * 0.1f;
            // float trueY = i * 0.1f;

            // Uncomment below for sinusoidal motion
            float t = i * 0.1f;
            float trueX = t;
            float trueY = (float) Math.Sin(t);
            
            // Add some noise to the true position
            double noiseScale = 1;

            Vector2 measurement = new Vector2(
                trueX + (float)((rand.NextDouble() - 0.5) * noiseScale),
                trueY + (float)((rand.NextDouble() - 0.5) * noiseScale)
            );

            // ======================
            // end noisy measurements
            // ======================

            // =====================================================
            // Real sensor input would look something like this
            // Vector2 measurement = new Vector2(sensorX, sensorY);
            // =====================================================


            Vector2 estimate = filter.Update(measurement);

            // Add to arrays
            trueX_List.Add(trueX);
            trueY_List.Add(trueY);

            measuredX_List.Add(measurement.X);
            measuredY_List.Add(measurement.Y);

            estimatedX_List.Add(estimate.X);
            estimatedY_List.Add(estimate.Y);

            Console.WriteLine($"True: ({trueX:F2}, {trueY:F2}), Measured: ({measurement.X:F2}, {measurement.Y:F2}), Estimated: ({estimate.X:F2}, {estimate.Y:F2})");

        }

        // Create the plot
        Console.WriteLine("Creating plot...");

        var plt = new ScottPlot.Plot();

        var a = plt.Add.Scatter(measuredX_List.ToArray(), measuredY_List.ToArray());
        a.LegendText = "Measured";

        var b = plt.Add.Scatter(estimatedX_List.ToArray(), estimatedY_List.ToArray());
        b.LegendText = "Estimated";

        // var c = plt.Add.Scatter(trueX_List.ToArray(), trueY_List.ToArray());
        // c.LegendText = "True";
  
        plt.Title("Kalman Filter: True vs Measured vs Estimated Positions");
        plt.XLabel("X Position");
        plt.YLabel("Y Position");

        plt.ShowLegend();

        plt.SavePng("kalman_filter_plot.png", 800, 800);

    }

}

public class KalmanFilter
{
    private Matrix4x4 A; // State transition matrix
    private Matrix4x4 H; // Observation matrix
    private Matrix4x4 Q; // Process noise covariance
    private Matrix4x4 R; // Measurement noise covariance
    private Matrix4x4 P; // Estimate error covariance
    private Vector4 x; // State estimate

    /// <summary>
    /// 
    /// Kalman filter implementation for 2D position tracking. <br/> <br/>
    /// 
    /// Args: <br/> <br/>
    /// <param name="dt">
    /// <b>dt</b>: Time step, influences magnitude of prediction step. <br/> <br/>
    /// </param>
    /// 
    /// <param name="processNoise">
    /// <b>processNoise</b>: Process noise, used in Q, + increases uncertainty in prediction so trusts measurements over model predictions. <br/> <br/>
    /// </param>
    /// 
    /// <param name="measurementNoise">
    /// <b>measurementNoise</b>: Measurement noise, used in R, + increases uncertainty in measurements so trusts model predictions over measurements. <br/> <br/>
    /// </param>
    ///
    /// <br/> <br/>
    /// - If processNoise > measurementNoise, filter favors measurements. <br/>
    /// - Tune hyperparameters based on application and sensor characteristics. <br/> <br/>
    /// 
    /// <param name="initialX">
    /// <b>initialX</b>: Initial X position. Default is 0. <br/> <br/>
    /// </param>
    /// 
    /// <param name="initialY">
    /// <b>initialY</b>: Initial Y position. Default is 0. <br/> <br/>
    /// </param>
    /// 
    /// </summary>
    public KalmanFilter(float dt, float processNoise, float measurementNoise, float initialX = 0, float initialY = 0)
    {
        // Initialize matrices
        A = new Matrix4x4(
            1, 0, dt, 0,
            0, 1, 0, dt,
            0, 0, 1, 0,
            0, 0, 0, 1
        );

        H = new Matrix4x4(
            1, 0, 0, 0,
            0, 1, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0
        );

        Q = Matrix4x4.CreateScale(processNoise);
        R = Matrix4x4.CreateScale(measurementNoise);
        P = Matrix4x4.Identity;
        // x = Vector4.Zero;
        x = new Vector4(initialX, initialY, 0, 0);
    }

    public Vector2 Update(Vector2 measurement)
    {
        // Predict
        x = Vector4.Transform(x, A);
        P = Matrix4x4.Multiply(Matrix4x4.Multiply(A, P), Matrix4x4.Transpose(A)) + Q;

        // Update
        var y = new Vector4(measurement.X, measurement.Y, 0, 0) - Vector4.Transform(x, H);
        var S = Matrix4x4.Multiply(Matrix4x4.Multiply(H, P), Matrix4x4.Transpose(H)) + R;
        
        // Invert S
        Matrix4x4 SInverse;
        if (!Matrix4x4.Invert(S, out SInverse))
        {
            // Handle inversion failure (S is not invertible)
            Console.WriteLine("Matrix inversion failed.");
            return new Vector2(x.X, x.Y);
        }

        var K = Matrix4x4.Multiply(Matrix4x4.Multiply(P, Matrix4x4.Transpose(H)), SInverse);

        x += Vector4.Transform(y, K);
        P = Matrix4x4.Multiply(Matrix4x4.Identity - Matrix4x4.Multiply(K, H), P);

        return new Vector2(x.X, x.Y);
    }
}