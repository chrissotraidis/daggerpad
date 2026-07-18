#import <GameController/GameController.h>
#import "UnityAppController.h"
#import "UI/UnityViewControllerBase.h"

#include <atomic>

static std::atomic<float> DaggerPadPointerDeltaX(0.0f);
static std::atomic<float> DaggerPadPointerDeltaY(0.0f);
static std::atomic<bool> DaggerPadPointerConnected(false);
static std::atomic<bool> DaggerPadPointerButtons[3];
static BOOL DaggerPadShouldLockPointer = NO;

static void DaggerPadAccumulate(std::atomic<float>& target, float value)
{
    float current = target.load(std::memory_order_relaxed);
    while (!target.compare_exchange_weak(current, current + value, std::memory_order_relaxed))
    {
    }
}

static void DaggerPadConfigureMouse(GCMouse *mouse)
{
    if (mouse == nil)
    {
        DaggerPadPointerConnected.store(false, std::memory_order_relaxed);
        NSLog(@"DaggerPad pointer disconnected.");
        return;
    }

    DaggerPadPointerConnected.store(true, std::memory_order_relaxed);
    NSLog(@"DaggerPad pointer connected through GameController.framework.");
    GCMouseInput *input = mouse.mouseInput;
    input.mouseMovedHandler = ^(GCMouseInput *mouseInput, float deltaX, float deltaY) {
        DaggerPadAccumulate(DaggerPadPointerDeltaX, deltaX);
        DaggerPadAccumulate(DaggerPadPointerDeltaY, deltaY);
    };

    input.leftButton.valueChangedHandler = ^(GCControllerButtonInput *button, float value, BOOL pressed) {
        DaggerPadPointerButtons[0].store(pressed, std::memory_order_relaxed);
    };

    if (input.rightButton != nil)
    {
        input.rightButton.valueChangedHandler = ^(GCControllerButtonInput *button, float value, BOOL pressed) {
            DaggerPadPointerButtons[1].store(pressed, std::memory_order_relaxed);
        };
    }

    if (input.middleButton != nil)
    {
        input.middleButton.valueChangedHandler = ^(GCControllerButtonInput *button, float value, BOOL pressed) {
            DaggerPadPointerButtons[2].store(pressed, std::memory_order_relaxed);
        };
    }
}

@interface DaggerPadPointerBridge : NSObject
@end

@implementation DaggerPadPointerBridge

+ (void)load
{
    dispatch_async(dispatch_get_main_queue(), ^{
        NSNotificationCenter *notifications = NSNotificationCenter.defaultCenter;
        [notifications addObserverForName:GCMouseDidConnectNotification
                                   object:nil
                                    queue:NSOperationQueue.mainQueue
                               usingBlock:^(NSNotification *notification) {
            DaggerPadConfigureMouse((GCMouse *)notification.object);
        }];
        [notifications addObserverForName:GCMouseDidDisconnectNotification
                                   object:nil
                                    queue:NSOperationQueue.mainQueue
                               usingBlock:^(NSNotification *notification) {
            DaggerPadConfigureMouse(GCMouse.current);
        }];
        DaggerPadConfigureMouse(GCMouse.current);
    });
}

@end

@implementation UnityDefaultViewController (DaggerPadPointerLock)

- (BOOL)prefersPointerLocked
{
    return DaggerPadShouldLockPointer;
}

@end

extern "C"
{
    int DaggerPadPointerIsConnected()
    {
        return DaggerPadPointerConnected.load(std::memory_order_relaxed) ? 1 : 0;
    }

    void DaggerPadPointerConsumeDelta(float *deltaX, float *deltaY)
    {
        *deltaX = DaggerPadPointerDeltaX.exchange(0.0f, std::memory_order_relaxed);
        *deltaY = DaggerPadPointerDeltaY.exchange(0.0f, std::memory_order_relaxed);
    }

    int DaggerPadPointerGetButton(int button)
    {
        if (button < 0 || button >= 3)
            return 0;
        return DaggerPadPointerButtons[button].load(std::memory_order_relaxed) ? 1 : 0;
    }

    void DaggerPadPointerSetLock(int shouldLock)
    {
        BOOL nextValue = shouldLock ? YES : NO;
        if (DaggerPadShouldLockPointer == nextValue)
            return;

        DaggerPadShouldLockPointer = nextValue;
        dispatch_async(dispatch_get_main_queue(), ^{
            UIViewController *controller = GetAppController().rootViewController;
            [controller setNeedsUpdateOfPrefersPointerLocked];
        });
    }
}
