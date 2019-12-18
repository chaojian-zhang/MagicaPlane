# Motivation

1. (Cause) MagicaVoxel is great for **visualization and rendering**, but I don't quite enjoy how its editing works. 
2. (Reason) One might try to use custom editor then generate directly to `.vox` format, that doesn't work, or generate directly to `.schematic` format, that doesn't work as well. One might expect to use only **MagicaVoxel View** - but none of the formats it claims to support seems to have and well-defined specification I can find.
3. (Alternative) There is **FileToVox** project, which unfortunately contains too many irrelavant code for my purpose.
4. (Definition) This tool (was originally conceived to) converts sequence of same-dimension `.png` into a MagicaVoxel shader, which when executed can have the effect of generating a corresponding `.vox` object - thus allowing a **plane based editing** experience.
	* But that is troublesome and require additional photo editing tools and coloring is troublesome, so instead we use *headless* `.csv` based approach.
5. (Feature) Naturally, to workaround the 126x126x126 size limit, we will support breaking the volumes down into seperate pieces.
6. (Future) Ideally we support better ways to exploit MagicaVoxel's `.vox` file format or MagicaVoxel Viewer more directly (e.g. support its *sparse oct tree* formats).
7. (Future) I just don't like the way MagicaVoxel's documentation, specification, and format design, ideally I would make my own alternative solution, but to compare with MagicaVoxel will require too much work.

# Remaining Problems

1. I don't even know the language format for **shader** file - does it support global variables, arrays, multi-dimension arrays? GL Shading language, per [here](https://github.com/CodingEric/Erics-MagicaVoxel-Shaders)?
2. After some test, it turns out with this method (probably due to restriction of GLSL arrays) we can feed a maximum of **15x15x15** grid, and nothing more! I am not happy with this.

# Usage

1. Draw your desired shapes in a sequence of *headless* `.csv` files to represent same-dimension layers for final voxel design, filling each cell a **target material name**; Those sequence files shall be named with numerical filenames, and will be ordered from lowest as bottom layers to highest as top layers, exact numerical values don't matter, only the relative order matters; See **sample layer** in **Reference** below.
2. Prepare a sepearte *headless* `.csv` file named `material.csv` which have two columns: **name**, and **value**, where "value" maps to index in the pallete (from `1`-`255`); See **sample material** in **Reference** below.
3. Put all those files in a common folder;
4. Execute MP and pass in folder path;
5. MP will generate a shader file for you to consume: you need to prepare the corresponding **cube dimension** and **clean the cube** before executing the shader.

# Formats for Material Value

Index of Color palette as you will use for Material property. Notice if a grid cell is empty, you need to specify null explicitly with index 0 (see example below).

# Implementation

The basic mechanism for this is simple: a large const array will be defined for each `vec3` position of the volume, then inside `map()` function we will just sample from it.

Example:

```glsl
const float volume[5] = float[5](1, 2, 3, 4, 5);

float map(vec3 v) {
	int index = int(mod(v.x, 5));
	return volume[index];
}
```

# Reference

**Sample Layer**

Something like `1.csv` and `2.csv` defines a `5 (row) x 3(col) x 2 (height)` volume (which corresponds to a total of `5x3x2=30` values):

```csv
ground,ground,ground
ground,ground,ground
ground,ground,ground
ground,ground,ground
ground,ground,ground
```

```csv
empty,ground,empty
ground,ground,ground
ground,light,ground
ground,ground,ground
empty,ground,empty
```

**Sample Material**

`material.csv`:

```csv
empty, 0
ground,1
light,5
```