using System;

class SimpleKalmanFilter
{
    public double Q = 0.00001;
    public double R = 0.01;
    private double P = 1, X = 0, K;

    private void MeasurementUpdate()
    {
        K = (P + Q) / (P + Q + R);
        P = R * K;
    }

    public double Update(double measurement)
    {
        MeasurementUpdate();
        double result = X + (measurement - X) * K;
        X = result;

        return result;
    }

    public double UpdateRounding(double measurement)
    {
        return Math.Round(Update(measurement));
    }
}