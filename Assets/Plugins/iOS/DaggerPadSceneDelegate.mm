#import <UIKit/UIKit.h>
#import "UnityAppController.h"

// Apps built with the iOS 27 SDK must start through UIKit scenes or iOS 27 closes
// them at launch. Unity 2022.3's app controller still expects the app-level
// lifecycle, which UIKit stops sending once an app declares scenes. This delegate
// (named in Info.plist by DaggerPadIosBuild) attaches Unity's window to the
// application scene and forwards the scene lifecycle to the app controller.
@interface DaggerPadSceneDelegate : UIResponder <UIWindowSceneDelegate>
@end

@implementation DaggerPadSceneDelegate

- (void)attachUnityWindowToScene:(UIScene *)scene
{
    if (![scene isKindOfClass:[UIWindowScene class]])
        return;
    UIWindow *window = GetAppController().window;
    if (window != nil && window.windowScene != scene)
        window.windowScene = (UIWindowScene *)scene;
}

- (void)scene:(UIScene *)scene willConnectToSession:(UISceneSession *)session options:(UISceneConnectionOptions *)connectionOptions
{
    [self attachUnityWindowToScene:scene];
}

- (void)sceneDidBecomeActive:(UIScene *)scene
{
    // Starts Unity on first activation (it then picks this scene for its window).
    [GetAppController() applicationDidBecomeActive:UIApplication.sharedApplication];
    [self attachUnityWindowToScene:scene];
}

- (void)sceneWillResignActive:(UIScene *)scene
{
    [GetAppController() applicationWillResignActive:UIApplication.sharedApplication];
}

- (void)sceneWillEnterForeground:(UIScene *)scene
{
    [GetAppController() applicationWillEnterForeground:UIApplication.sharedApplication];
}

- (void)sceneDidEnterBackground:(UIScene *)scene
{
    [GetAppController() applicationDidEnterBackground:UIApplication.sharedApplication];
}

@end
