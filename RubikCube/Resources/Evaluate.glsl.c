#version 430 core

const int N = 4;
const int SIZE = 2;
const int XFORM_SIZE = N * N;
const int GENES_COUNT = 30;
const int POPULATION_COUNT = 1000;
const int PLANES_COUNT = N * (N - 1) / 2;
const float C = (SIZE - 1) / 2;

layout(local_size_x = 1) in;

struct XForm
{
    float[XFORM_SIZE] M;
    float[N] Origin;
};

layout(std430, binding = 0) buffer Cubies {
    XForm cubies[];
};

layout(std430, binding = 1) buffer SolvedCubies {
    int solvedCubies[];
};

layout(std430, binding = 2) buffer ActiveCubies {
    int activeCubies[];
};

layout(std430, binding = 3) buffer WorkCubies {
    XForm workCubies[];
};

layout(std140, binding = 4) uniform planes
{
    vec2 Planes[PLANES_COUNT];
};

struct Chromosome
{
    int Genes[GENES_COUNT];
    int MoveCount;
    float Fitness;
};

layout(std140, binding = 5) buffer Population
{
    Chromosome population[POPULATION_COUNT];
};

int getAngle(float cosA, float sinA)
{
    if (cosA > 0.5) return 0;
    if (sinA > 0.5) return 1;
    if (cosA < -0.5) return 2;
    if (sinA < -0.5) return 3;
    return 0;
}

vec2 setAngle(int angle)
{
    float cosA = 0;
    float sinA = 0;
    switch (angle)
    {
    case 0: cosA = 1; break;
    case 1: sinA = 1; break;
    case 2: cosA = -1; break;
    case 3: sinA = -1; break;
    }
    return vec2(cosA, sinA);
}

float[N] RotateVec(float[N] v, int axis1, int axis2, vec2 rot)
{
    float a = v[axis1];
    float b = v[axis2];
    v[axis1] = rot.x * a - rot.y * b;
    v[axis2] = rot.y * a + rot.x * b;
    return v;
}

float[XFORM_SIZE] RotateMat(float[XFORM_SIZE] mat, int axis1, int axis2, vec2 rot)
{
    for (int i = axis1, j = axis2; i < XFORM_SIZE; i += N, j += N)
    {
        float a = mat[i];
        float b = mat[j];
        mat[i] = rot.x * a - rot.y * b;
        mat[j] = rot.y * a + rot.x * b;
    }
    return mat;
}

void Rotate(int i, int plane, int angle)
{
    int axis1 = int(Planes[plane].x);
    int axis2 = int(Planes[plane].y);
    vec2 rot = setAngle(angle);
    workCubies[i].M = RotateMat(workCubies[i].M, axis1, axis2, rot);
    workCubies[i].Origin = RotateVec(workCubies[i].Origin, axis1, axis2, rot);
}

vec2[PLANES_COUNT] getEulerAngles(float[XFORM_SIZE] M)
{
    vec2[PLANES_COUNT] angles;
    float[XFORM_SIZE] A = M;
    for (int i = 0; i < Planes.length(); i++)
    {
        int axis1 = int(Planes[i].x);
        int axis2 = int(Planes[i].y);
        float a = A[axis1 + axis1 * N];
        float b = A[axis2 + axis1 * N];
        float r = sqrt(a * a + b * b);
        if (r < 0.1)
            angles[i] = vec2(1, 0);
        else
        {
            float cosA = a / r;
            float sinA = b / r;
            A = RotateMat(A, axis1, axis2, vec2(cosA, -sinA));
            angles[i] = vec2(cosA, sinA);
        }
    }
    return angles;
}

int getState(XForm transform)
{
    vec2[PLANES_COUNT] EulerAngles = getEulerAngles(transform.M);
    int state = 0;
    int shift = (PLANES_COUNT - 1) << 1;
    for (int i = 0; i < PLANES_COUNT; i++)
    {
        int angle = getAngle(EulerAngles[i].x, EulerAngles[i].y);
        state |= angle << shift;
        shift -= 2;
    }
    return state;
}

int getRotationCount(int state)
{
    int RotationCount = 0;
    for (int i = 0; i < PLANES_COUNT; i++)
    {
        if ((state & 3) > 0)
            RotationCount++;
        state >>= 2;
    }
    return RotationCount;
}

struct Move
{
    int Axis;
    int Slice;
    int Plane;
    int Angle;
};

Move getMove(int code)
{
    int size = SIZE * PLANES_COUNT * 3;
    int axis = code / size;
    code -= axis * size;
    size /= SIZE;
    int slice = code / size;
    code -= slice * size;
    size /= PLANES_COUNT;
    int plane = code / size;
    code -= plane * size;
    int angle = code;
    return Move(axis, slice, plane, angle);
}

float evaluateCubies()
{
    float score = 0;
    int scrambled = 0;
    float maxClusterState = (1 << 2 * PLANES_COUNT) * activeCubies.length() * N;
    for (int i = 0; i < activeCubies.length(); i++)
    {
        int state = getState(workCubies[activeCubies[i]]);
        if (state != 0)
        {
            score += maxClusterState + state + (getRotationCount(state) << PLANES_COUNT);
            scrambled++;
        }
    }
    score /= maxClusterState * (activeCubies.length() + 1);
    if (scrambled == 1)
        score *= 2;
    for (int i = 0; i < solvedCubies.length(); i++)
    {
        int state = getState(workCubies[solvedCubies[i]]);
        if (state != 0)
            score++;
    }
    return 100 * score;
}

void Turn(Move move)
{
    for (int i = 0; i < workCubies.length(); i++)
    {
        float v = round(workCubies[i].Origin[move.Axis] + C);
        if (int(v) == move.Slice)
            Rotate(i, move.Plane, move.Angle + 1);
    }       
}
 
void main()
{
    int specimenID = int(gl_GlobalInvocationID.x);
    Chromosome specimen = population[specimenID];
    float bestFitness = 100 * cubies.length();
		workCubies = cubies;
		for (int i = 0; i < GENES_COUNT; i++)
		{
				Turn(getMove(specimen.Genes[i]));
        float fitness = evaluateCubies();
				if (fitness < bestFitness)
				{
						bestFitness = fitness;
						specimen.MoveCount = i + 1;
				}
		}
    specimen.Fitness = bestFitness;
    population[specimenID] = specimen;
}