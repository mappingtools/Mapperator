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

    private static (Tensors, Tensors) DownsampleBlock3(Tensors x, int nFilters) {
        var f = DoubleConvBlock(x, nFilters);
        var p = keras.layers.MaxPooling2D(2).Apply(f);

        return (f, p);
    }

    private static Tensors UpsampleBlock3(Tensors x, Tensors convFeatures, int nFilters) {
        x = keras.layers.Conv2DTranspose(nFilters, 3, strides: 2, output_padding: "same").Apply(x);
        x = keras.layers.BatchNormalization().Apply(x);
        x = keras.layers.Concatenate().Apply(new Tensors(x, convFeatures));
        x = DoubleConvBlock(x, nFilters);

        return x;
    }

    public static IModel GetModel3(Shape imgSize) {
        var inputs = keras.Input(imgSize);
        var t1EmbedInput = keras.Input(64);
        var t2EmbedInput = keras.Input(64);

        var t1Emb = keras.layers.Dense(128).Apply(t1EmbedInput);
        t1Emb = keras.layers.Swish().Apply(t1Emb);
        var t2Emb = keras.layers.Dense(128).Apply(t2EmbedInput);
        t2Emb = keras.layers.Swish().Apply(t2Emb);

        var tEmb = keras.layers.Concatenate().Apply(new Tensors(t1Emb, t2Emb));
        tEmb = keras.layers.Dense(512).Apply(tEmb);
        tEmb = keras.layers.Reshape(new Shape(1, 1, 512)).Apply(tEmb);

        var (f1, p1) = DownsampleBlock3(inputs, 32);
        var (f2, p2) = DownsampleBlock3(p1, 64);
        var (f3, p3) = DownsampleBlock3(p2, 128);
        var (f4, p4) = DownsampleBlock3(p3, 256);

        var bottleneck = DoubleConvBlock(p4, 512);
        var bottleneckEmb = keras.layers.Add().Apply(new Tensors(bottleneck, tEmb));

        var u1 = UpsampleBlock3(bottleneckEmb, f4, 256);
        var u2 = UpsampleBlock3(u1, f3, 128);
        var u3 = UpsampleBlock3(u2, f2, 64);
        var u4 = UpsampleBlock3(u3, f1, 32);

        var output = keras.layers.Conv2D(1, 3, padding: "same", activation: "linear").Apply(u4);
        output = keras.layers.Flatten().Apply(output);
        output = keras.layers.Softmax().Apply(output);

        return keras.Model(new Tensors(inputs, t1EmbedInput, t2EmbedInput), output, name: "U-Net3");
    }
}