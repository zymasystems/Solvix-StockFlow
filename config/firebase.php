<?php

require_once __DIR__ . '/../vendor/autoload.php';

use Kreait\Firebase\Factory;
use Dotenv\Dotenv;

// Load .env
$dotenv = Dotenv::createImmutable(__DIR__ . '/..');
$dotenv->load();

$serviceAccountPath = __DIR__ . '/../' . $_ENV['FIREBASE_KEY_PATH'];

if (!file_exists($serviceAccountPath)) {
    die("Error: Firebase key file not found!");
}

$factory = (new Factory)
    ->withServiceAccount($serviceAccountPath);

$auth = $factory->createAuth();

function getFirebaseAuth()
{
    global $auth;
    return $auth;
}