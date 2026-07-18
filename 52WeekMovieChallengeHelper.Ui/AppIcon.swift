import AppKit
import Foundation

func hex(_ v: UInt32) -> NSColor {
    NSColor(srgbRed: CGFloat((v >> 16) & 0xFF) / 255.0,
            green: CGFloat((v >> 8) & 0xFF) / 255.0,
            blue: CGFloat(v & 0xFF) / 255.0,
            alpha: 1.0)
}

let boardColor = hex(0x1F1E26)
let creamColor = hex(0xF7F3E9)

func draw(size s: CGFloat) {
    guard let ctx = NSGraphicsContext.current?.cgContext else { return }

    let inset = s * 0.055
    let rect = CGRect(x: inset, y: inset, width: s - 2 * inset, height: s - 2 * inset)
    let radius = rect.width * 0.2237
    let R = rect.width

    // Amber tile
    NSGraphicsContext.saveGraphicsState()
    NSBezierPath(roundedRect: rect, xRadius: radius, yRadius: radius).addClip()
    NSGradient(colors: [hex(0xF6C64B), hex(0xE0941F)])!.draw(in: rect, angle: -90)

    // Slate body
    let bodyRect = CGRect(x: rect.minX + R * 0.115,
                          y: rect.minY + R * 0.145,
                          width: R * 0.770,
                          height: R * 0.455)
    boardColor.setFill()
    NSBezierPath(roundedRect: bodyRect, xRadius: R * 0.035, yRadius: R * 0.035).fill()

    // Hinged clapper stick, angled up to the right
    let barH = R * 0.150
    let barRect = CGRect(x: bodyRect.minX, y: bodyRect.maxY + R * 0.020,
                         width: bodyRect.width, height: barH)

    ctx.saveGState()
    ctx.translateBy(x: barRect.minX, y: barRect.minY)
    ctx.rotate(by: 9.0 * .pi / 180.0)
    ctx.translateBy(x: -barRect.minX, y: -barRect.minY)

    let barPath = NSBezierPath(roundedRect: barRect, xRadius: R * 0.028, yRadius: R * 0.028)
    boardColor.setFill()
    barPath.fill()

    // Diagonal cream stripes across the stick
    NSGraphicsContext.saveGraphicsState()
    barPath.addClip()
    let stripeW = barH * 0.52
    let slant = barH * 0.40
    creamColor.setFill()
    var x = barRect.minX - slant
    while x < barRect.maxX + slant {
        let stripe = NSBezierPath()
        stripe.move(to: CGPoint(x: x, y: barRect.minY))
        stripe.line(to: CGPoint(x: x + slant, y: barRect.maxY))
        stripe.line(to: CGPoint(x: x + slant + stripeW, y: barRect.maxY))
        stripe.line(to: CGPoint(x: x + stripeW, y: barRect.minY))
        stripe.close()
        stripe.fill()
        x += stripeW * 2
    }
    NSGraphicsContext.restoreGraphicsState()
    ctx.restoreGState()

    NSGraphicsContext.restoreGraphicsState()

    // "52" on the slate
    let fontSize = R * 0.335
    let para = NSMutableParagraphStyle()
    para.alignment = .center
    let text = NSAttributedString(string: "52", attributes: [
        .font: NSFont.systemFont(ofSize: fontSize, weight: .heavy),
        .foregroundColor: creamColor,
        .paragraphStyle: para,
        .kern: -fontSize * 0.03
    ])
    let textSize = text.size()
    text.draw(in: CGRect(x: bodyRect.minX,
                         y: bodyRect.midY - textSize.height / 2,
                         width: bodyRect.width,
                         height: textSize.height))
}

func render(_ px: Int, to url: URL) {
    let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: px, pixelsHigh: px,
                               bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true,
                               isPlanar: false, colorSpaceName: .deviceRGB,
                               bytesPerRow: 0, bitsPerPixel: 0)!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
    draw(size: CGFloat(px))
    NSGraphicsContext.restoreGraphicsState()
    try! rep.representation(using: .png, properties: [:])!.write(to: url)
}

let outDir = URL(fileURLWithPath: CommandLine.arguments[1])
for px in [16, 32, 64, 128, 256, 512, 1024] {
    render(px, to: outDir.appendingPathComponent("icon_\(px).png"))
}
print("rendered")
