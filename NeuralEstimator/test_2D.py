import torch
import torch.nn as nn
import torch.optim as optim
import pandas as pd
import numpy as np
import matplotlib.pyplot as plt
from sklearn.model_selection import train_test_split
from sklearn.metrics import mean_absolute_error, mean_squared_error, r2_score

# --- Configuration ---
DATA_FILE = "dataset_2d.csv"
MODEL_FILE = "uncertainty_model_2d.pth"
EPOCHS = 500
BATCH_SIZE = 64
LEARNING_RATE = 0.001

# --- 1. Define the Neural Network ---
class UncertaintyNet(nn.Module):
    def __init__(self):
        super(UncertaintyNet, self).__init__()
        # Input: 5 features (A_h, B_min_x, B_min_y, B_max_x, B_max_y)
        # Output: 1 value (U_f)
        self.model = nn.Sequential(
            nn.Linear(5, 64),
            nn.ReLU(),
            nn.Linear(64, 64),
            nn.ReLU(),
            nn.Linear(64, 32),
            nn.ReLU(),
            nn.Linear(32, 1)
        )

    def forward(self, x):
        return self.model(x)

def main():
    # --- 2. Load and Preprocess Data ---
    print(f"Loading data from {DATA_FILE}...")
    try:
        df = pd.read_csv(DATA_FILE)
    except FileNotFoundError:
        print("Error: CSV file not found. Run the C# generator first!")
        return

    # Split Features (X) and Target (y)
    X = df.iloc[:, 0:5].values.astype(np.float32)
    y = df.iloc[:, 5].values.astype(np.float32).reshape(-1, 1)

    # Split into Train (80%) and Test (20%) sets
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42)

    # Convert to PyTorch Tensors
    X_train_tensor = torch.tensor(X_train)
    y_train_tensor = torch.tensor(y_train)
    X_test_tensor = torch.tensor(X_test)
    y_test_tensor = torch.tensor(y_test)

    # --- 3. Setup Training ---
    model = UncertaintyNet()
    criterion = nn.MSELoss() # Mean Squared Error Loss for Regression
    optimizer = optim.Adam(model.parameters(), lr=LEARNING_RATE)

    print("Starting training...")
    train_losses = []
    
    # --- 4. Training Loop ---
    for epoch in range(EPOCHS):
        model.train()
        
        # Forward pass
        predictions = model(X_train_tensor)
        loss = criterion(predictions, y_train_tensor)
        
        # Backward pass
        optimizer.zero_grad()
        loss.backward()
        optimizer.step()
        
        train_losses.append(loss.item())

        if (epoch+1) % 50 == 0:
            print(f"Epoch [{epoch+1}/{EPOCHS}], Loss: {loss.item():.6f}")

    # --- 5. Evaluation on Test Data ---
    model.eval()
    with torch.no_grad():
        test_predictions = model(X_test_tensor)
        test_loss = criterion(test_predictions, y_test_tensor)
    
    # Convert back to numpy for metrics
    y_true = y_test
    y_pred = test_predictions.numpy()

    # Metrics
    mae = mean_absolute_error(y_true, y_pred)
    rmse = np.sqrt(mean_squared_error(y_true, y_pred))
    r2 = r2_score(y_true, y_pred)

    print("\n--- Validation Results (Test Set) ---")
    print(f"Mean Absolute Error (MAE): {mae:.6f}")
    print(f"Root Mean Squared Error (RMSE): {rmse:.6f}")
    print(f"R-squared Score: {r2:.4f}")

    # --- 6. Save Model ---
    torch.save(model.state_dict(), MODEL_FILE)
    print(f"\nModel saved to {MODEL_FILE}")

    # Optional: Plot
    plt.scatter(y_true, y_pred, alpha=0.5)
    plt.xlabel("Actual U_f")
    plt.ylabel("Predicted U_f")
    plt.title("Prediction vs Ground Truth")
    plt.plot([y_true.min(), y_true.max()], [y_true.min(), y_true.max()], 'r--')
    plt.show()

if __name__ == "__main__":
    main()