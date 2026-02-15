import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

# --- Configuration ---
DATA_FILE = "dataset_3d_1M_R.csv"
MODEL_FILE = "uncertainty_model_3d_LONGER_TRAINING.pth"
EPOCHS = 3000  # More epochs for 3D complexity
BATCH_SIZE = 1024
LEARNING_RATE = 0.001

# --- Neural Network (14 Inputs -> 1 Output) ---
class UncertaintyNet3D(nn.Module):
    def __init__(self):
        super(UncertaintyNet3D, self).__init__()
        self.model = nn.Sequential(
            nn.Linear(14, 128),  # Input layer: 14 features
            nn.ReLU(),
            nn.Linear(128, 128), # Hidden layer 1
            nn.ReLU(),
            nn.Linear(128, 64),  # Hidden layer 2
            nn.ReLU(),
            nn.Linear(64, 1)     # Output
        )

    def forward(self, x):
        return self.model(x)

def main():
    print(f"Loading 3D data from {DATA_FILE}...")
    try:
        df = pd.read_csv(DATA_FILE)
    except FileNotFoundError:
        print("Error: CSV not found.")
        return

    # Split Features (First 14 cols) and Target (Last col)
    X = df.iloc[:, 0:14].values.astype(np.float32)
    y = df.iloc[:, 14].values.astype(np.float32).reshape(-1, 1)

    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

    X_train_tensor = torch.tensor(X_train)
    y_train_tensor = torch.tensor(y_train)
    X_test_tensor = torch.tensor(X_test)
    y_test_tensor = torch.tensor(y_test)

    model = UncertaintyNet3D()
    criterion = nn.MSELoss()
    optimizer = optim.Adam(model.parameters(), lr=LEARNING_RATE)

    print("Starting 3D training...")
    for epoch in range(EPOCHS):
        model.train()
        predictions = model(X_train_tensor)
        loss = criterion(predictions, y_train_tensor)
        
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()

        if (epoch+1) % 100 == 0:
            print(f"Epoch [{epoch+1}/{EPOCHS}], Loss: {loss.item():.6f}")

    # Validation
    model.eval()
    with torch.no_grad():
        test_predictions = model(X_test_tensor)
    
    y_true = y_test
    y_pred = test_predictions.numpy()

    mae = mean_absolute_error(y_true, y_pred)
    r2 = r2_score(y_true, y_pred)

    print("\n--- 3D Validation Results ---")
    print(f"MAE: {mae:.6f} (radians)")
    print(f"MAE: {mae * (180/3.14159):.4f} (degrees)")
    print(f"R2 Score: {r2:.4f}")

    # Export ONNX
    dummy = torch.randn(1, 14)
    torch.onnx.export(model, dummy, "uncertainty_model_3d.onnx", 
                      input_names=['input'], output_names=['output'])
    print("Model exported to ONNX.")

if __name__ == "__main__":
    main()