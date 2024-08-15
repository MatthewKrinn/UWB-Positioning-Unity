////////////////////////////////////////////////////////////////////////////////////////////////
/*
Implementation of generic Kalman filter for 2D position tracking.
Extended Kalman Filter and Unscented Kalman Filter are more advanced versions of this filter.
I chose the generic filter because EKF is more computationally intensive,
and might not be the best choice for a POC visualizer.

note:
Kalman Filter is good for Gaussian noise. Assuming that is the type of noise in the sensor,
ie, the noise is normally distributed and not like (1,1.1,1.03,5,0.9...), should be good.

In the future, maybe implement an Interacting Multiple Model filter for Daytona tracking
pit crew as their modes change from running to active working.
- However, this implementation takes into account position and velocity (change in position),
    so it might be good enough for the pit crew tracking.
*/
////////////////////////////////////////////////////////////////////////////////////////////////


using System;
using UnityEngine;


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
        // Lol, copilot didn't know that unity matrix initialization uses columns instead of rows
        A = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(dt, 0, 1, 0),
            new Vector4(0, dt, 0, 1)
        );

        H = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 0, 0),
            new Vector4(0, 0, 0, 0)
        );
        

        Q = Matrix4x4.Scale(new Vector3(processNoise, processNoise, processNoise));
        R = Matrix4x4.Scale(new Vector3(measurementNoise, measurementNoise, measurementNoise));
        P = Matrix4x4.identity;
        // x = Vector4.Zero;
        x = new Vector4(initialX, initialY, 0, 0);
    }

    public Vector2 UpdateFilter(Vector2 measurement)
    {
        // Predict

        // x = Vector4.Transform(x, A);
        x = A * x;

        // P = Matrix4x4.Multiply(Matrix4x4.Multiply(A, P), Matrix4x4.Transpose(A)) + Q;
        P = AddMatrices(A * P * A.transpose, Q);
    

        // Update

        // var y = new Vector4(measurement.X, measurement.Y, 0, 0) - Vector4.Transform(x, H);
        var y = new Vector4(measurement.x, measurement.y, 0, 0) - (H * x);


        // var S = Matrix4x4.Multiply(Matrix4x4.Multiply(H, P), Matrix4x4.Transpose(H)) + R;
        var S = AddMatrices(H * P * H.transpose, R);
        
        // Invert S
        Matrix4x4 SInverse;
        if (S.determinant != 0) 
        {
            SInverse = S.inverse;
        } 
        else 
        {
            // Handle inversion failure (S is not invertible)
            Debug.LogWarning("Matrix inversion failed.");
            // Return vector only transformed with A, no Kalman gain
            return new Vector2(x.x, x.y);
        }


        // var K = Matrix4x4.Multiply(Matrix4x4.Multiply(P, Matrix4x4.Transpose(H)), SInverse);
        var K = P * H.transpose * SInverse;

        x += K * y;


        // P = Matrix4x4.Multiply(Matrix4x4.Identity - Matrix4x4.Multiply(K, H), P);
        P = SubMatrices(Matrix4x4.identity, K * H) * P;

        // return new Vector2(x.X, x.Y);
        return new Vector2(x.x, x.y);
    }

    Matrix4x4 AddMatrices(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = a[i] + b[i];
        }
        return result;
    }

    Matrix4x4 SubMatrices(Matrix4x4 a, Matrix4x4 b)
    {
        Matrix4x4 result = new Matrix4x4();
        for (int i = 0; i < 16; i++)
        {
            result[i] = a[i] - b[i];
        }
        return result;
    }
}