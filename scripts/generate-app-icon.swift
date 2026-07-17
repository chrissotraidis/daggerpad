#!/usr/bin/env swift

import AppKit

let size = 1024
let output = CommandLine.arguments.dropFirst().first ?? "Assets/Brand/DaggerPadAppIcon.png"
let bounds = NSRect(x: 0, y: 0, width: size, height: size)

guard let bitmap = NSBitmapImageRep(
    bitmapDataPlanes: nil,
    pixelsWide: size,
    pixelsHigh: size,
    bitsPerSample: 8,
    samplesPerPixel: 4,
    hasAlpha: true,
    isPlanar: false,
    colorSpaceName: .deviceRGB,
    bytesPerRow: 0,
    bitsPerPixel: 0
) else {
    fatalError("Could not create the app-icon bitmap")
}

NSGraphicsContext.saveGraphicsState()
NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bitmap)

NSColor(calibratedRed: 0.025, green: 0.035, blue: 0.065, alpha: 1).setFill()
NSBezierPath(rect: bounds).fill()
NSGradient(colors: [
    NSColor(calibratedRed: 0.035, green: 0.075, blue: 0.14, alpha: 1),
    NSColor(calibratedRed: 0.025, green: 0.035, blue: 0.065, alpha: 1),
    NSColor(calibratedRed: 0.12, green: 0.045, blue: 0.08, alpha: 1),
])?.draw(in: bounds, angle: -55)

for (rect, color) in [
    (NSRect(x: -170, y: 570, width: 620, height: 620), NSColor(calibratedRed: 0.11, green: 0.55, blue: 0.75, alpha: 0.09)),
    (NSRect(x: 620, y: -150, width: 540, height: 540), NSColor(calibratedRed: 0.95, green: 0.3, blue: 0.24, alpha: 0.08)),
] {
    color.setFill()
    NSBezierPath(ovalIn: rect).fill()
}

let tablet = NSBezierPath(roundedRect: NSRect(x: 194, y: 142, width: 636, height: 740), xRadius: 116, yRadius: 116)
tablet.lineWidth = 42
NSColor(calibratedWhite: 0.94, alpha: 0.09).setFill()
tablet.fill()
NSColor(calibratedRed: 0.79, green: 0.86, blue: 0.94, alpha: 0.92).setStroke()
tablet.stroke()

let screen = NSBezierPath(roundedRect: NSRect(x: 253, y: 205, width: 518, height: 614), xRadius: 76, yRadius: 76)
NSGradient(colors: [
    NSColor(calibratedRed: 0.05, green: 0.13, blue: 0.22, alpha: 0.82),
    NSColor(calibratedRed: 0.10, green: 0.055, blue: 0.105, alpha: 0.92),
])?.draw(in: screen, angle: -70)

let blade = NSBezierPath()
blade.move(to: NSPoint(x: 512, y: 246))
blade.line(to: NSPoint(x: 438, y: 405))
blade.line(to: NSPoint(x: 469, y: 657))
blade.curve(to: NSPoint(x: 512, y: 686), controlPoint1: NSPoint(x: 473, y: 677), controlPoint2: NSPoint(x: 492, y: 686))
blade.curve(to: NSPoint(x: 555, y: 657), controlPoint1: NSPoint(x: 532, y: 686), controlPoint2: NSPoint(x: 551, y: 677))
blade.line(to: NSPoint(x: 586, y: 405))
blade.close()
blade.lineWidth = 15

let shadow = NSShadow()
shadow.shadowColor = NSColor.black.withAlphaComponent(0.55)
shadow.shadowBlurRadius = 30
shadow.shadowOffset = NSSize(width: 0, height: -14)
shadow.set()
NSGradient(colors: [
    NSColor(calibratedRed: 0.98, green: 0.99, blue: 1, alpha: 1),
    NSColor(calibratedRed: 0.48, green: 0.62, blue: 0.75, alpha: 1),
])?.draw(in: blade, angle: 0)
NSColor(calibratedRed: 0.11, green: 0.20, blue: 0.30, alpha: 1).setStroke()
blade.stroke()

let guardPath = NSBezierPath(roundedRect: NSRect(x: 356, y: 640, width: 312, height: 66), xRadius: 33, yRadius: 33)
guardPath.lineWidth = 13
NSColor(calibratedRed: 0.96, green: 0.38, blue: 0.24, alpha: 1).setFill()
guardPath.fill()
NSColor(calibratedRed: 0.36, green: 0.10, blue: 0.10, alpha: 1).setStroke()
guardPath.stroke()

let grip = NSBezierPath(roundedRect: NSRect(x: 464, y: 682, width: 96, height: 128), xRadius: 42, yRadius: 42)
grip.lineWidth = 13
NSColor(calibratedRed: 0.16, green: 0.24, blue: 0.33, alpha: 1).setFill()
grip.fill()
NSColor(calibratedRed: 0.88, green: 0.79, blue: 0.61, alpha: 1).setStroke()
grip.stroke()

let pommel = NSBezierPath(ovalIn: NSRect(x: 466, y: 776, width: 92, height: 92))
pommel.lineWidth = 13
NSColor(calibratedRed: 0.96, green: 0.38, blue: 0.24, alpha: 1).setFill()
pommel.fill()
NSColor(calibratedRed: 0.36, green: 0.10, blue: 0.10, alpha: 1).setStroke()
pommel.stroke()

NSGraphicsContext.restoreGraphicsState()

guard let png = bitmap.representation(using: .png, properties: [:]) else {
    fatalError("Could not encode the app icon")
}

let outputURL = URL(fileURLWithPath: output)
try FileManager.default.createDirectory(at: outputURL.deletingLastPathComponent(), withIntermediateDirectories: true)
try png.write(to: outputURL, options: .atomic)
print("Generated \(output) (\(size)x\(size))")
