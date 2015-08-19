#import "ViewController.h"
#import <objc/runtime.h>

@implementation ViewController

static const void* myExtraDataTag = "assocTag";

- (void)viewDidLoad {
    [super viewDidLoad];
    // Do any additional setup after loading the view, typically from a nib.
    
    NSString* extraData = [NSString stringWithFormat:@"foo%d\n", 1];
    
    objc_setAssociatedObject(self.view, myExtraDataTag, extraData, OBJC_ASSOCIATION_RETAIN_NONATOMIC);
}

-(void)viewWillAppear:(BOOL)animated {
    NSString* extraData = objc_getAssociatedObject(self.view, myExtraDataTag);
    if(extraData) {
        NSLog(@"got extra data of %@", extraData);
    }
}

- (void)didReceiveMemoryWarning {
    [super didReceiveMemoryWarning];
    // Dispose of any resources that can be recreated.
}

@end
