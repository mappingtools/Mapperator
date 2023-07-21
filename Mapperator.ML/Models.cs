using Tensorflow.Keras;
using Tensorflow.Keras.Engine;

namespace Mapperator.ML;

using static Tensorflow.Binding;
using static Tensorflow.KerasApi;
using Tensorflow;
using Tensorflow.NumPy;

public class Models {
    private static Tensors DoubleConvBlock(Tensors x, int nFilters) {
        x = keras.layers.Conv2D(nFilters, 3, padding: "same", activation: "relu", kernel_initializer: "he_normal").Apply(x);
        x = keras.layers.BatchNormalization().Apply(x);
        x = keras.layers.Conv2D(nFilters, 3, padding: "same", activation: "relu", kernel_initializer: "he_normal").Apply(x);
        x = keras.layers.BatchNormalization().Apply(x);

        return x;
    }

    private static (Tensors, Tensors) DownsampleBlock(Tensors x, int nFilters) {
        var f = DoubleConvBlock(x, nFilters);
        var p = keras.layers.MaxPooling2D(2).Apply(f);
        p = keras.layers.Dropout(0.3f).Apply(p);

        return (f, p);
    }

    private static Tensors UpsampleBlock(Tensors x, Tensors convFeatures, int nFilters) {
        x = keras.layers.Conv2DTranspose(nFilters, 3, strides: 2, output_padding: "same").Apply(x);
        x = keras.layers.BatchNormalization().Apply(x);
        x = keras.layers.Concatenate().Apply(new Tensors(x, convFeatures));
        x = keras.layers.Dropout(0.3f).Apply(x);
        x = DoubleConvBlock(x, nFilters);

        return x;
    }

    public static IModel GetModel2(Shape imgSize) {
        var inputs = keras.Input(imgSize);

        var (f1, p1) = DownsampleBlock(inputs, 64);
        var (f2, p2) = DownsampleBlock(p1, 128);
        var (f3, p3) = DownsampleBlock(p2, 256);
        var (f4, p4) = DownsampleBlock(p3, 512);

        var bottleneck = DoubleConvBlock(p4, 1024);

        var u1 = UpsampleBlock(bottleneck, f4, 512);
        var u2 = UpsampleBlock(u1, f3, 256);
        var u3 = UpsampleBlock(u2, f2, 128);
        var u4 = UpsampleBlock(u3, f1, 64);

        var outputs = keras.layers.Conv2D(1, 1, padding: "same", activation: "linear").Apply(u4);
        outputs = keras.layers.Flatten().Apply(outputs);
        outputs = keras.layers.Softmax().Apply(outputs);

        return keras.Model(inputs, outputs, name: "U-Net2");
    }
}