<?php
// Dashboard welcome page
require_once __DIR__ . '/includes/auth-check.php';
?>
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Dashboard</title>
    <style>
        body {
            margin: 0;
            font-family: Arial, sans-serif;
            background-color: #f4f6f8;
            color: #333;
        }
        .dashboard-container {
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            padding: 40px;
            text-align: center;
        }
        .dashboard-card {
            background: #ffffff;
            border-radius: 12px;
            box-shadow: 0 16px 40px rgba(15, 23, 42, 0.08);
            max-width: 700px;
            width: 100%;
            padding: 40px;
        }
        .dashboard-card h1 {
            margin: 0 0 16px;
            font-size: 2.5rem;
            color: #111827;
        }
        .dashboard-card p {
            margin: 0 0 24px;
            font-size: 1rem;
            line-height: 1.7;
            color: #4b5563;
        }
        .dashboard-card .highlight {
            color: #2563eb;
            font-weight: 600;
        }
        .dashboard-actions {
            display: flex;
            gap: 12px;
            justify-content: center;
            flex-wrap: wrap;
        }
        .dashboard-actions a {
            display: inline-block;
            padding: 12px 24px;
            border-radius: 8px;
            text-decoration: none;
            color: #ffffff;
            background-color: #2563eb;
            transition: background-color 0.2s ease;
        }
        .dashboard-actions a:hover {
            background-color: #1d4ed8;
        }
    </style>
</head>
<body>
    <div class="dashboard-container">
        <div class="dashboard-card">
            <h1>Welcome to Your Dashboard</h1>
            <p>Hello! This is your central dashboard where you can manage your projects, review reports, and stay on top of your tasks. Use the links below to get started.</p>
            <div class="dashboard-actions">
                <a href="#">View Overview</a>
                <a href="#">Manage Settings</a>
                <a href="#">Reports</a>
                <a href="./logout.php" style="background-color: #dc2626;">Logout</a>
            </div>
        </div>
    </div>
</body>
</html>
